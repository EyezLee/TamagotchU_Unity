using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MetaData
{
    public string message;
    public float value;
}

public class TimedEntry
{
    public string message;
    public float value;
    public DateTime timestamp;

    public override string ToString()
    {
        return $"[{timestamp:HH:mm:ss.fff}] {message}: {value}";
    }
}

public class SocketReceiver : MonoBehaviour
{
    public int port = 5005;

    private TcpListener _listener;
    private volatile bool _listening = false;
    private readonly object clientsLock = new object();

    // Map from client IP to list of timed entries (reuse lists for performance)
    private readonly Dictionary<string, List<TimedEntry>> clientTimedEntries = new Dictionary<string, List<TimedEntry>>();

    private float cleanupInterval = 3f; // seconds
    private float cleanupTimer = 0f;

    void Start()
    {
        _listening = true;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Thread listenerThread = new Thread(ListenForClients);
        listenerThread.IsBackground = true;
        listenerThread.Start();

        Debug.Log($"Listening on port {port}");
    }

    void OnDestroy()
    {
        _listening = false;
        _listener.Stop();
    }

    private void ListenForClients()
    {
        while (_listening)
        {
            if (!_listener.Pending())
            {
                Thread.Sleep(10);
                continue;
            }

            TcpClient client = _listener.AcceptTcpClient();
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            Debug.Log($"Client connected: {clientIP}");

            Thread clientThread = new Thread(() => HandleClient(client, clientIP));
            clientThread.IsBackground = true;
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client, string clientIP)
    {
        NetworkStream stream = client.GetStream();

        try
        {
            while (_listening && client.Connected)
            {
                byte[] lenBuf = ReadExactly(stream, 4);
                if (lenBuf == null) break;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lenBuf);
                int metaLength = BitConverter.ToInt32(lenBuf, 0);

                byte[] metaBuf = ReadExactly(stream, metaLength);
                if (metaBuf == null) break;

                string metaJson = Encoding.UTF8.GetString(metaBuf);
                MetaData meta = JsonUtility.FromJson<MetaData>(metaJson);

                lock (clientsLock)
                {
                    if (!clientTimedEntries.TryGetValue(clientIP, out var entries))
                    {
                        entries = new List<TimedEntry>();
                        clientTimedEntries[clientIP] = entries;
                    }

                    entries.Add(new TimedEntry
                    {
                        message = meta.message,
                        value = meta.value,
                        timestamp = DateTime.UtcNow
                    });

                    // Remove entries older than 3 seconds
                    DateTime cutoff = DateTime.UtcNow.AddSeconds(-cleanupInterval);
                    entries.RemoveAll(e => e.timestamp < cutoff);

                    // Sort only if needed, entries mostly added in order, so this is cheap
                    entries.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Client {clientIP} disconnected or error: {ex.Message}");
        }
        finally
        {
            client.Close();
            lock (clientsLock)
            {
                clientTimedEntries.Remove(clientIP);
            }
            Debug.Log($"Disconnected client {clientIP}");
        }
    }

    void Update()
    {
        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= cleanupInterval)
        {
            cleanupTimer = 0f;
            CleanOldEntries();
        }
    }

    private void CleanOldEntries()
    {
        lock (clientsLock)
        {
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-3);
            foreach (var entries in clientTimedEntries.Values)
            {
                entries.RemoveAll(e => e.timestamp < cutoff);
            }
        }
    }

    /// <summary>
    /// Gets the current valid TimedEntry list for a client IP.
    /// Reuses a single list per client IP to reduce GC pressure.
    /// </summary>
    public void GetClientDataList(string clientIP, List<TimedEntry> outList)
    {
        outList.Clear();
        lock (clientsLock)
        {
            if (clientTimedEntries.TryGetValue(clientIP, out var entries))
            {
                outList.AddRange(entries);
            }
            else
            {
                Debug.LogWarning($"Client IP '{clientIP}' not found in data queues.");
            }
        }
    }

    private byte[] ReadExactly(NetworkStream stream, int len)
    {
        byte[] buffer = new byte[len];
        int read = 0;
        while (read < len)
        {
            int r = stream.Read(buffer, read, len - read);
            if (r == 0)
                return null;
            read += r;
        }
        return buffer;
    }
}
