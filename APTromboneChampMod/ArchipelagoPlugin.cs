using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BaboonAPI.Hooks;
using BaboonAPI.Hooks.Initializer;
using BaboonAPI.Hooks.Tracks;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;

namespace APTromboneChampMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ch.offbeatwit.baboonapi.plugin")]
public class ArchipelagoPlugin : BaseUnityPlugin {
    public static ArchipelagoPlugin Instance;
    internal new static ManualLogSource Logger;

    public static APSettings WorldSettings = new();
    public static Track[] FilteredTracks = [];
    public static Track? GoalTrack = null;

    public static ArchipelagoSession APSession = null;
    public static int APTeam = -1, APSlot = -1;
    public static Version VERSION = new(0, 6, 7);

    public static List<long> ITEMS = [];
    public static List<long> SENT_LOCS = [];
    
    public static void OnReceivedItems(List<long> items) {
        ITEMS.AddRange(items);
        bool updateTracks = false;
        bool refreshHints = false;
        foreach (long item in items) {
            if ((item > 0L && item < 1000L) || item is 1001L or 1004L || item > 1010L) updateTracks = true;
            if (item is 1001L or 1004L or 1011L) refreshHints = true;
        }
        if (updateTracks) OnTrackAvailabilityChanged();
        else if (refreshHints) ; // TODO
    }

    public static bool IsTrackAvailable(Track track) {
        if (APSession is null || APSlot == -1) return false;
        
        if (!FilteredTracks.Contains(track)) return false;
        if (WorldSettings.TrackGating && !ITEMS.Contains(track.ID)) return false;
        if (track.Difficulty > WorldSettings.MinDiff) {
            if (WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
                if (!ITEMS.Contains(1010L + track.Difficulty)) return false;
            }
            if (WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
                int diff = track.Difficulty - WorldSettings.MinDiff;
                if (diff > ITEMS.Count(id => id == 1011L)) return false;
            }
        }
        if (track.Name == WorldSettings.GoalTrack) {
            int hotDogs = ITEMS.Count(id => id == 1004L);
            if (hotDogs < WorldSettings.HotDogs) return false;
            int rank = ITEMS.Count(id => id == 1001L);
            int req = WorldSettings.InitialRating - WorldSettings.GoalRating;
            if (rank < req) return false;
        }
        return true;
    }

    public static void SendTrack(Track track, bool beaten) {
        if (APSession is null || APSlot == -1) return;
        if (!IsTrackAvailable(track)) return; // precaution
        long[] IDs = beaten ? [track.ID, track.ID + 1000L] : [track.ID];
        APSession.Locations.CompleteLocationChecksAsync(IDs);
        if (beaten) {
            // goal logic
            if (WorldSettings.GoalTracks == 0) {
                if (GoalTrack.HasValue) {
                    if (track.ID == GoalTrack.Value.ID) APSession.SetGoalAchieved();
                }
                else Logger.LogWarning("Goal tracks is 0 and no goal track is set!");
            }
            else {
                int numBeaten = SENT_LOCS.Count(id => id > 1000L);
                if (!SENT_LOCS.Contains(track.ID + 1000L)) numBeaten++;
                if (numBeaten >= WorldSettings.GoalTracks) APSession.SetGoalAchieved();
            }
        }
    }

    public static bool CanHint() {
        if (APSession is null || APSlot == -1) return false;
        return APSession.RoomState.HintPoints >= APSession.RoomState.HintCost;
    }

    public static void TryHintTrack(Track track) {
        if (!WorldSettings.TrackGating) return;
        if (!CanHint()) return;
        // TODO: verify not already hinted
        string loc = APSession.Locations.GetLocationNameFromId(track.ID);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void TryHintDifficulty(int diff) {
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.OFF) return;
        if (!CanHint()) return;
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) diff = 1;
        // TODO: verify not already hinted
        string loc = APSession.Locations.GetLocationNameFromId(1010L + diff);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void TryHintRankReduction() {
        if (WorldSettings.GoalRating == WorldSettings.InitialRating) return;
        if (!CanHint()) return;
        // TODO: verify not already hinted
        string loc = APSession.Locations.GetLocationNameFromId(1001L);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void TryHintHotDog() {
        if (WorldSettings.HotDogs == 0) return;
        if (!CanHint()) return;
        // TODO: verify not already hinted
        string loc = APSession.Locations.GetLocationNameFromId(1004L);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void ConnectToAP(string host, int port, string slot, string pass) {
        Logger.LogInfo($"Connecting to {host}:{port}");
        APSession?.Socket.DisconnectAsync();
        APSession = null;
        APSlot = -1;
        OnWorldSettingsChanged();
        try {
            APSession = ArchipelagoSessionFactory.CreateSession(host, port);
            APSession.Items.ItemReceived += (helper) => {
                List<long> items = [];
                ItemInfo item;
                while (helper.Any()) {
                    item = helper.PeekItem();
                    items.Add(item.ItemId);
                    helper.DequeueItem();
                }
                OnReceivedItems(items);
            };
            APSession.Locations.CheckedLocationsUpdated += (helper) => {
                SENT_LOCS.Clear();
                SENT_LOCS.AddRange(helper);
                OnTrackAvailabilityChanged();
            };
            // TODO: add event handlers before connecting
            LoginResult result = APSession.TryConnectAndLogin( // TODO: async version
                "Trombone Champ", slot,
                ItemsHandlingFlags.AllItems,
                VERSION, [], null, pass, true
            );
            if (!result.Successful)
            {
                // TODO: show errors
                Logger.LogWarning($"Failed to connect to {host}:{port}");
                LoginFailure failure = (LoginFailure)result;
                foreach (string error in failure.Errors) Logger.LogWarning($"    {error}");
                foreach (ConnectionRefusedError error in failure.ErrorCodes) Logger.LogWarning($"    {error}");
                return;
            }

            LoginSuccessful success = (LoginSuccessful)result;
            APTeam = success.Team;
            APSlot = success.Slot;
            Logger.LogInfo($"Successfully connected to {host}:{port} - Team: {APTeam}, Slot: {APSlot}");
            
            WorldSettings.GoalTracks = int.Parse(success.SlotData["goal"].ToString());
            WorldSettings.GoalTrack = success.SlotData["goal_track"].ToString();
            WorldSettings.GoalRating = int.Parse(success.SlotData["rating"].ToString());
            WorldSettings.InitialRating = int.Parse(success.SlotData["rating_start"].ToString());
            WorldSettings.EasyTrackGap = int.Parse(success.SlotData["easy_track"].ToString());
            WorldSettings.HotDogs = int.Parse(success.SlotData["hot_dogs"].ToString());
            WorldSettings.ExtraHotDogs = int.Parse(success.SlotData["extra_hot_dogs"].ToString());
            WorldSettings.TrackGating = int.Parse(success.SlotData["track_gating"].ToString()) > 0;
            WorldSettings.DifficultyGating = (APSettings.DiffGateType)int.Parse(success.SlotData["difficulty_gating"].ToString());
            WorldSettings.MinDiff = int.Parse(success.SlotData["min_diff"].ToString());
            WorldSettings.MaxDiff = int.Parse(success.SlotData["max_diff"].ToString());
            WorldSettings.Unsafe = int.Parse(success.SlotData["unsafe"].ToString()) != 0;
            WorldSettings.Celeste = int.Parse(success.SlotData["celeste"].ToString()) != 0;
            WorldSettings.PizzaTower = int.Parse(success.SlotData["pizza_tower"].ToString()) != 0;
            WorldSettings.UndertaleDeltarune = int.Parse(success.SlotData["undertale_deltarune"].ToString()) != 0;
            WorldSettings.RemovedTracks = ((JArray)success.SlotData["removed_tracks"]).ToObject<string[]>();
            OnWorldSettingsChanged();
        }
        catch (Exception e) {
            Logger.LogError($"Unusual error: {e.Message}");
            Logger.LogError(e.StackTrace);
        }
    }

    public static void OnWorldSettingsChanged() {
        // called when connecting to an AP session
        if (APSession is not null) {
            // TODO: load settings from world
        }
        else {
            // specific settings to only show Baboons! track, for testing
            WorldSettings.MinDiff = 6;
            WorldSettings.MaxDiff = 6;
            WorldSettings.Unsafe = false;
            WorldSettings.RemovedTracks = [
                "Chop Waltz", "Funiculi Funicula", "Hello! Ma Baby", "Rosamunde", "SkaBIRD", "Skeleton Rag"
            ];
            // TODO: show a disconnected notice
        }
        FilteredTracks = APTracks.GetTrackList(WorldSettings).ToArray();
        GoalTrack = APTracks.GetGoalTrack(WorldSettings, FilteredTracks);
        OnTrackAvailabilityChanged();
        TrackReloader.ReloadAll(null);
    }

    public static Track[] AvailableTracks = [];

    public static void OnTrackAvailabilityChanged() {
        // called when receiving items that might change what tracks are playable
        AvailableTracks = FilteredTracks.Where(IsTrackAvailable).ToArray();
        // TODO: make sure a track cant be played if not available
    }
    
    private void Awake() {
        Instance = this;
        
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is running at version {MyPluginInfo.PLUGIN_VERSION}");
        Logger.LogInfo($"Hello World!");
        GameInitializationEvent.Register(Info, TryInitialize);
    }

    private void TryInitialize() {
        TrackCollectionRegistrationEvent.EVENT.Register(new TrackCollectionListener());
        ConnectToAP("localhost", 38281, "HexiTrombone", null);
    }
}