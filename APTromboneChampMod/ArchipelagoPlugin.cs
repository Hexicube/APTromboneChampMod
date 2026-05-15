using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Models;
using BaboonAPI.Hooks.Initializer;
using BaboonAPI.Hooks.Tracks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace APTromboneChampMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ch.offbeatwit.baboonapi.plugin")]
public class ArchipelagoPlugin : BaseUnityPlugin {
    private static Harmony _harmony = new Harmony("archipelago");
    
    public static ArchipelagoPlugin Instance;
    internal new static ManualLogSource Logger;

    // Logging
    private string uri = "archipelago.gg";
    private string port = "38281"; // port is later on parsed to int
    private string slotname = "";
    private string password = "";

    // UI
    public int curGUI = -1;
    private Rect windowRect = new Rect(20, 20, 500, 300);
    
    private void Awake() {
        Instance = this;
        
        Logger = base.Logger;
        GameInitializationEvent.Register(Info, TryInitialize);
    }

    private void TryInitialize() {
        TrackCollectionRegistrationEvent.EVENT.Register(new TrackCollectionListener());
        
        _harmony.PatchAll();
    }

    [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Awake))]
    class ScoreScreenChecker {
        static void Postfix() {
            // scores should be available at this stage
            // 135% = wall break, 100% = S, 80% = A, 60% = B, 40% = C, 20% = D
            float scorePct = GlobalVariables.gameplay_scoreperc;
            int achievedRating = (int)(scorePct / .2f) - 2; // C = 0, S = 3
            if (achievedRating > 3) { // at least 120%
                if (GlobalVariables.gameplay_perfect) achievedRating = 5; // perfect track
                else if (scorePct >= 1.35) achievedRating = 4; // wall break
                else achievedRating = 3; // S
            }
            bool beaten = achievedRating >= APHandler.GetRequiredRating();
            Track track = APHandler.AvailableTracks.FirstOrDefault(track => track.Name == GlobalVariables.chosen_track_data.trackname_short);
            if (track.Name == GlobalVariables.chosen_track_data.trackname_short) { // make sure it exists
                Logger.LogInfo($"Track end screen: {GlobalVariables.chosen_track_data.trackname_short}");
                Logger.LogInfo($"Score: {scorePct}");
                Logger.LogInfo($"Rating: {achievedRating}");
                Logger.LogInfo($"Beaten: {beaten}");
                
                // precaution: prevent submitting a track you dont have
                if (!APHandler.IsTrackAvailable(track)) {
                    Logger.LogInfo("Submission blocked, track is not available.");
                    return;
                }
                
                APHandler.SendTrack(track, beaten);
            }
        }
    }

    [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.clickPlay))]
    class PreventPlayingTrack {
        static bool Prefix(LevelSelectController __instance) {
            if (APHandler.APSlot == -1) return true;
            
            Track? track = APHandler.FindTrack(__instance.alltrackslist[__instance.songindex].trackname_short);
            if (!track.HasValue) return false;
            if (!APHandler.IsTrackAvailable(track.Value)) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.populateSongNames))]
    class ChangeSongDescriptionAndDisablePlay {
        static void Postfix(LevelSelectController __instance) {
            // detect AP collections and look for edge-case of single track being Warm-Up
            global::TrackCollection current = GlobalVariables.all_track_collections[GlobalVariables.chosen_collection_index];
            if (current._unique_id is "AP" or "AP_checks") {
                if (current._trackcount == 1 && current.all_tracks[0].trackname_short == "Warm-Up") {
                    bool isFallback = false;
                    if (current._unique_id == "AP" && APHandler.FilteredTracks.All(track => track.Name != "Warm-Up")) isFallback = true;
                    if (current._unique_id == "AP_checks" && APHandler.AvailableTracks.All(track => track.Name != "Warm-Up")) isFallback = true;
                    if (isFallback) {
                        if (current._unique_id == "AP") {
                            if (APHandler.APSlot == -1) __instance.songdesctext.text = "Not connected to AP!\nPress F1 to open connection manager.";
                            else __instance.songdesctext.text = "AP song list is empty!\nReport this with your config included.";
                        }
                        else {
                            if (APHandler.APSlot == -1) __instance.songdesctext.text = "Not connected to AP!\nPress F1 to open connection manager.";
                            else __instance.songdesctext.text = "No songs left to play.\nMaybe hint something?";
                        }
                        __instance.playbtn.enabled = false;
                        __instance.playbtn.gameObject.SetActive(false);
                        return;
                    }
                }
            }
            
            bool canPlay = false;
            if (APHandler.APSlot == -1) canPlay = true;
            else {
                Track? track = APHandler.FindTrack(__instance.alltrackslist[__instance.songindex].trackname_short);
                if (track.HasValue) {
                    TrackHints hints = APHandler.GetTrackHints(track.Value);
                    
                    bool hasPlay = APHandler.APSentLocations.Contains(track.Value.ID);
                    bool hasBeat = APHandler.APSentLocations.Contains(track.Value.ID + 1000L);
                    StringBuilder str = new();

                    if (APHandler.GoalTrack.HasValue && APHandler.GoalTrack.Value.ID == track.Value.ID)
                        str.Append("This is the goal track, beat it to win!\n");
                    
                    if (APHandler.IsTrackAvailable(track.Value)) {
                        if (hasPlay && hasBeat) str.Append("Track already beaten.");
                        else {
                            canPlay = true;
                            str.Append("Can play this track!");
                            str.Append($"\nRequired rating: {APHandler.GetRatingString(APHandler.GetRequiredRating())}");
                            if (!hasPlay) str.Append($"\nPlay reward: {APHandler.FormatItemHint(hints.PlayReward)}");
                            if (!hasBeat) str.Append($"\nBeat reward: {APHandler.FormatItemHint(hints.BeatReward)}");
                        }
                    }
                    else {
                        str.Append("Track locked.");
                        
                        if (APHandler.GoalTrack.HasValue && APHandler.GoalTrack.Value.ID == track.Value.ID) {
                            // check hot dogs
                            int reqHotDogs = APHandler.WorldSettings.HotDogs;
                            if (reqHotDogs > 0) {
                                int foundHotDogs = APHandler.APFoundItems.Count(id => id == 1004L);
                                if (foundHotDogs < reqHotDogs) {
                                    str.Append($"\nHot dogs: {foundHotDogs}/{reqHotDogs}");
                                    
                                    // TODO: show these hints somewhere else, text is far too small otherwise (especially with many hot dogs)
                                    //Hint[] hotDogHints = APHandler.GetHotDogHints();
                                    //foreach (Hint hint in hotDogHints)
                                    //    str.Append($"\nHot Dog: {APHandler.FormatItemHint(hint)}");
                                }
                            }
                        }
                        
                        if (APHandler.WorldSettings.TrackGating) {
                            if (!APHandler.APFoundItems.Contains(track.Value.ID))
                                str.Append($"\nTrack unlock: {APHandler.FormatLocationString(hints.TrackUnlock)}");
                        }
                        
                        if (APHandler.WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
                            if (!APHandler.APFoundItems.Contains(track.Value.Difficulty + 1010L))
                                str.Append($"\nDifficulty {track.Value.Difficulty}: {APHandler.FormatLocationString(hints.DifficultyUnlocks[0])}");
                        }

                        if (APHandler.WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
                            int req = track.Value.Difficulty - APHandler.WorldSettings.MinDiff;
                            if (req > 0) {
                                int found = APHandler.APFoundItems.Count(id => id == 1011L);
                                if (found < req) {
                                    str.Append($"\nDifficulty unlocks: {found}/{req}");
                                    foreach (Hint hint in hints.DifficultyUnlocks)
                                        str.Append($"\nProgressive Difficulty: {APHandler.FormatLocationString(hint)}");
                                }
                            }
                        }
                    }

                    __instance.songdesctext.text = str.ToString();
                }
                else __instance.songdesctext.text = "Not an AP track.";
            }
            __instance.playbtn.enabled = canPlay;
            __instance.playbtn.gameObject.SetActive(canPlay);
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            if (curGUI == -1) {
                if (APHandler.APSlot == -1) curGUI = 0;
                else curGUI = 1;
            }
            else curGUI = -1;
        }
    }

    void OnGUI() {
        // show that the AP mod loaded ok
        GUI.DrawTexture(new Rect(10, 10, 40, 40), Base64Images.Archipelago.texture);

        if (curGUI != -1) windowRect = GUI.Window(curGUI, windowRect, WindowHandler, "Archipelago Menu");
    }

    void WindowHandler(int ID) {
        switch (ID) {
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

    void ShowLoginWindow() {
        GUILayout.Label("Server URI: ");
        uri = GUILayout.TextField(uri, GUILayout.Width(200));

        GUILayout.Label("Server Port: ");
        port = GUILayout.TextField(port, GUILayout.Width(200));

        GUILayout.Label("Server Slotname: ");
        slotname = GUILayout.TextField(slotname, GUILayout.Width(200));

        GUILayout.Label("Server Password (Optional): ");
        password = GUILayout.PasswordField(password, '*', GUILayout.Width(200));

        if (GUILayout.Button("Connect Archipelago", GUILayout.Height(30)) && int.TryParse(port.Trim(), out int portInt)) 
        {
            APHandler.ConnectToAP(uri, portInt, slotname, password);
        }

        if (GUILayout.Button("Close")) curGUI = -1;
    }

    void ShowTrackerWindow() {
        GUILayout.Label("Connected to AP server.");
        
        GUILayout.Space(10);

        Track[] tracks = APTracks.GetTrackList(APHandler.WorldSettings).ToArray();
        
        string goal;
        if (APHandler.WorldSettings.GoalTracks == 0) goal = $"Goal track: {APHandler.WorldSettings.GoalTrack}";
        else {
            int numBeaten = tracks.Count(track => APHandler.APSentLocations.Contains(track.ID + 1000L));
            goal = $"Beat tracks: {numBeaten}/{APHandler.WorldSettings.GoalTrack}";
        }
        GUILayout.Label(goal);

        if (APHandler.WorldSettings.HotDogs > 0) {
            int found = APHandler.APFoundItems.Count(id => id == 1004L);
            if (APHandler.WorldSettings.ExtraHotDogs > 0) {
                int total = APHandler.WorldSettings.HotDogs + APHandler.WorldSettings.ExtraHotDogs;
                GUILayout.Label($"Hot Dogs: {found}/{APHandler.WorldSettings.HotDogs} ({total})");
            }
            else GUILayout.Label($"Hot Dogs: {found}/{APHandler.WorldSettings.HotDogs}");
        }

        string[] ratings = ["C", "B", "A", "S"];
        GUILayout.Label($"Required rating: {ratings[APHandler.GetRequiredRating()]}");

        if (APHandler.WorldSettings.InitialRating != APHandler.WorldSettings.GoalRating) {
            int total = APHandler.WorldSettings.InitialRating - APHandler.WorldSettings.GoalRating;
            int found = APHandler.APFoundItems.Count(id => id == 1001L);
            GUILayout.Label($"Rating Reduction items: {found}/{total}");
        }
        
        GUILayout.Space(10);

        if (APHandler.WorldSettings.TrackGating ||
            APHandler.WorldSettings.DifficultyGating != APSettings.DiffGateType.OFF) {
            if (APHandler.WorldSettings.TrackGating) {
                int found = tracks.Count(track => APHandler.APFoundItems.Contains(track.ID));
                GUILayout.Label($"Tracks unlocked: {found}/{tracks.Length}");
            }

            if (APHandler.WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
                List<int> unlockedDiffs = [APHandler.WorldSettings.MinDiff];
                for (int a = APHandler.WorldSettings.MinDiff + 1; a <= APHandler.WorldSettings.MaxDiff; a++) {
                    if (APHandler.APFoundItems.Contains(a + 1010L)) unlockedDiffs.Add(a);
                }
                GUILayout.Label($"Difficulties unlocked: {string.Join(", ", unlockedDiffs)}");
            }
            if (APHandler.WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
                int maxDiff = APHandler.WorldSettings.MinDiff + APHandler.APFoundItems.Count(id => id == 1011L);
                GUILayout.Label($"Max difficulty: {maxDiff}/{APHandler.WorldSettings.MaxDiff}");
            }
        
            GUILayout.Space(10);
        }

        if (GUILayout.Button("Disconnect")) {
            APHandler.APSession.Socket.DisconnectAsync();
            APHandler.APSlot = -1;
            curGUI = 0;
        }
        if (GUILayout.Button("Close")) curGUI = -1;
        
        if (GUILayout.Button("Hint Hot Dog")) APHandler.TryHintHotDog();
    }
}