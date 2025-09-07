using System.Collections.Generic;
using UnityEngine;

// Class combining TamaManager and its editable IPConfig
[System.Serializable]
public class TamaIPPair
{
    public TamaManager tamaManager;
    public string IPAddress;

    public TamaIPPair(TamaManager manager)
    {
        tamaManager = manager;
        IPAddress = tamaManager.frameRequester.pythonServerIp;
    }
}
public class DebugMenuManager : MonoBehaviour
{
    // Paired list of TamaManager and IPConfig objects
    public List<TamaIPPair> tamaPairs = new List<TamaIPPair>();

    private bool showDebugMenu = true;
    private Vector2 scrollPos;

    private void Update()
    {
        // Toggle debug UI with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            showDebugMenu = !showDebugMenu;
        }
    }

    void OnGUI()
    {
        if (!showDebugMenu) return;

        GUILayout.BeginArea(new Rect(20, 20, 400, 450), GUI.skin.window);
        GUILayout.Label("Debug Menu – Server/Client IPs");

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < tamaPairs.Count; ++i)
        {
            var pair = tamaPairs[i];
            GUILayout.Label($"TamaManager[{i}]");

            GUILayout.Label("TamaPager IP:");
            pair.IPAddress = GUILayout.TextField(pair.IPAddress ?? "", 25);

            GUILayout.Space(8);

            if (GUILayout.Button("Save"))
            {
                // Apply input back to TamaManager instance
                pair.tamaManager.tamapagerIP = pair.IPAddress;

                // Save to player prefs for persistence
                PlayerPrefs.SetString($"Tamapager_{i}_IP", pair.tamaManager.tamapagerIP);
                PlayerPrefs.Save();

                Debug.Log($"Saved TamaManager[{i}]: tamapager IP ='{pair.tamaManager.tamapagerIP}'");
            }

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); // Visual divider
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Close"))
        {
            showDebugMenu = false;
        }

        GUILayout.EndArea();
    }
}