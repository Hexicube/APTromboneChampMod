using BaboonAPI.Hooks.Initializer;
using BaboonAPI.Hooks.Tracks;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace APTromboneChampMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ch.offbeatwit.baboonapi.plugin")]
public class ArchipelagoPlugin : BaseUnityPlugin {
    public static ArchipelagoPlugin Instance;
    internal new static ManualLogSource Logger;

    // Logging
    private string slotname = "";
    private string uri = "";
    private string port = ""; // port is later on parsed to int
    private string password = "";

    // UI
    private bool showGui = false;
    private Rect windowRect = new Rect(20, 20, 500, 300);
    
    private void Awake() {
        Instance = this;
        
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is running at version {MyPluginInfo.PLUGIN_VERSION}");
        Logger.LogInfo($"Hello World!");
        GameInitializationEvent.Register(Info, TryInitialize);
    }

    private void TryInitialize() {
        TrackCollectionRegistrationEvent.EVENT.Register(new TrackCollectionListener());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showGui = !showGui;
        }

    }

    void OnGUI()
    {
        if (showGui) 
        {
            windowRect = GUI.Window(0, windowRect, ShowWindow, "Archipelago Menu");
        }
    }

    void ShowWindow(int windowID)
    {
        GUILayout.Label("Server URI: ");
        uri = GUILayout.TextField(uri, GUILayout.Width(200));

        GUILayout.Label("Server Port: ");
        port = GUILayout.TextField(port, GUILayout.Width(200));

        GUILayout.Label("Server Slotname: ");
        slotname = GUILayout.TextField(slotname, GUILayout.Width(200));

        GUILayout.Label("Server Password (Optional): ");
        password = GUILayout.PasswordField(password, '*', GUILayout.Width(200));

        if (GUILayout.Button("Connect Archipelago", GUILayout.Height(30)) && int.TryParse(port, out int portInt)) 
        {
            APHandler.ConnectToAP(uri, portInt, slotname, password);
        }
        
        if (GUILayout.Button("Close"))
            showGui = false;
    }
}