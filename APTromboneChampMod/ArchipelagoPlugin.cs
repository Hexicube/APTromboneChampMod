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
    private int curGUI = -1;
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
            if (curGUI == -1) curGUI = 0;
            else curGUI = -1;
        }
    }

    void OnGUI()
    {
        if (curGUI != -1) windowRect = GUI.Window(curGUI, windowRect, WindowHandler, "Archipelago Menu");
    }

    void WindowHandler(int ID)
    {
        switch (ID)
        {
            case 0:
                ShowLoginWindow();
                break;
            default:
                Logger.LogWarning($"Unknown GUI ID: {ID}");
                curGUI = -1;
                break;
        }
    }

    void ShowLoginWindow()
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

        if (GUILayout.Button("Close")) curGUI = -1;
    }
}