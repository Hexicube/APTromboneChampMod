using System.Collections.Generic;
using System.Linq;
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
    public int curGUI = -1;
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
            if (curGUI == -1)
            {
                if (APHandler.APSlot == -1) curGUI = 0;
                else curGUI = 1;
            }
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
            case 1:
                ShowTrackerWindow();
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

    void ShowTrackerWindow()
    {
        GUILayout.Label("Connected to AP server.");
        
        GUILayout.Space(10);

        Track[] tracks = APTracks.GetTrackList(APHandler.WorldSettings).ToArray();
        
        string goal;
        if (APHandler.WorldSettings.GoalTracks == 0) goal = $"Goal track: {APHandler.WorldSettings.GoalTrack}";
        else {
            int numBeaten = tracks.Count(track => APHandler.SENT_LOCS.Contains(track.ID + 1000L));
            goal = $"Beat tracks: {numBeaten}/{APHandler.WorldSettings.GoalTrack}";
        }
        GUILayout.Label(goal);

        string[] ratings = ["C", "B", "A", "S"];
        GUILayout.Label($"Required rating: {ratings[APHandler.GetRequiredRating()]}");

        if (APHandler.WorldSettings.InitialRating != APHandler.WorldSettings.GoalRating) {
            int total = APHandler.WorldSettings.InitialRating - APHandler.WorldSettings.GoalRating;
            int found = APHandler.ITEMS.Count(id => id == 1001L);
            GUILayout.Label($"Rating Reduction items: {found}/{total}");
        }
        
        GUILayout.Space(10);

        if (APHandler.WorldSettings.TrackGating ||
            APHandler.WorldSettings.DifficultyGating != APSettings.DiffGateType.OFF) {
            if (APHandler.WorldSettings.TrackGating) {
                int found = tracks.Count(track => APHandler.ITEMS.Contains(track.ID));
                GUILayout.Label($"Tracks unlocked: {found}/{tracks.Length}");
            }

            if (APHandler.WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
                List<int> unlockedDiffs = [APHandler.WorldSettings.MinDiff];
                for (int a = APHandler.WorldSettings.MinDiff + 1; a <= APHandler.WorldSettings.MaxDiff; a++) {
                    if (APHandler.ITEMS.Contains(a + 1010L)) unlockedDiffs.Add(a);
                }
                GUILayout.Label($"Difficulties unlocked: {string.Join(", ", unlockedDiffs)}");
            }
            if (APHandler.WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
                int maxDiff = APHandler.WorldSettings.MinDiff + APHandler.ITEMS.Count(id => id == 1011L);
                GUILayout.Label($"Max difficulty: {maxDiff}/{APHandler.WorldSettings.MaxDiff}");
            }
        
            GUILayout.Space(10);
        }

        if (APHandler.WorldSettings.HotDogs > 0) {
            int found = APHandler.ITEMS.Count(id => id == 1004L);
            if (APHandler.WorldSettings.ExtraHotDogs > 0) {
                int total = APHandler.WorldSettings.HotDogs + APHandler.WorldSettings.ExtraHotDogs;
                GUILayout.Label($"Hot Dogs: {found}/{APHandler.WorldSettings.HotDogs} ({total})");
            }
            else GUILayout.Label($"Hot Dogs: {found}/{APHandler.WorldSettings.HotDogs}");
            
            GUILayout.Space(10);
        }

        if (GUILayout.Button("Disconnect")) {
            APHandler.APSession.Socket.DisconnectAsync();
            APHandler.APSlot = -1;
            curGUI = 0;
        }
        if (GUILayout.Button("Close")) curGUI = -1;
    }
}