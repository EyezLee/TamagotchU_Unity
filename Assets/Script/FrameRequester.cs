using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class FrameRequester : MonoBehaviour
{
    public string pythonServerIp = "PYTHON_PC_IP"; // replace with real IP
    public int pythonPort = 6006;
    [SerializeField] Renderer targetRenderer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) // Change 'F' to your desired key
        {
            SendFrameRequest();
        }
    }

    void SendFrameRequest()
    {
        try
        {
            using (TcpClient client = new TcpClient(pythonServerIp, pythonPort))
            using (NetworkStream stream = client.GetStream())
            {
                // Simple request - can be "FRAME", or protocol as you want
                string request = "FRAME";
                byte[] requestBytes = Encoding.UTF8.GetBytes(request);
                stream.Write(requestBytes, 0, requestBytes.Length);

                // Read frame data back (e.g., as PNG bytes)
                // --- here you need to agree the receive format ---
                // For demo: Read first 4 bytes (length), then read that many bytes

                byte[] lenBuf = new byte[4];
                stream.Read(lenBuf, 0, 4);
                if (System.BitConverter.IsLittleEndian)
                    System.Array.Reverse(lenBuf);
                int imgLength = System.BitConverter.ToInt32(lenBuf, 0);
                byte[] imgBytes = new byte[imgLength];
                int read = 0;
                while (read < imgLength)
                    read += stream.Read(imgBytes, read, imgLength - read);

                // Load the image bytes into a Texture2D
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imgBytes))
                {
                    Debug.Log("Received frame from Python!");

                    // Assign the texture to the material on the target renderer
                    if (targetRenderer != null)
                    {
                        targetRenderer.material.mainTexture = tex;
                    }
                    else
                    {
                        Debug.LogWarning("targetRenderer is null. Assign a Renderer that has the material to display the texture.");
                    }
                }
                else
                {
                    Debug.LogError("Failed to load image bytes into texture.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"FrameRequest error: {ex}");
        }
    }
}
