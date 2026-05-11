using System;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using BaboonAPI.Hooks;
using BaboonAPI.Hooks.Initializer;
using BaboonAPI.Hooks.Tracks;
using BepInEx;
using BepInEx.Logging;
using JetBrains.Annotations;

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

    public static void ConnectToAP(string host, int port, string slot, string pass) {
        Logger.LogInfo($"Connecting to {host}:{port}");
        APSession?.Socket.DisconnectAsync();
        APSlot = -1;
        try {
            APSession = ArchipelagoSessionFactory.CreateSession(host, port);
            // TODO: add event handlers before connecting
            LoginResult result = APSession.TryConnectAndLogin(
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
            // TODO: success.SlotData (for item names)
        }
        catch (Exception e) {
            Logger.LogError($"Unusual error: {e.Message}");
            Logger.LogError(e.StackTrace);
        }
    }

    public void OnWorldSettingsChanged() {
        // called when connecting to an AP session
        if (APSession is not null) {
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
    }

    public static Track[] AvailableTracks = [];

    public void OnTrackAvailabilityChanged() {
        // called when receiving items that might change what tracks are playable
        // TODO: list of items to check against
        // TODO: list of locations to determine if a track has been completed
        AvailableTracks = FilteredTracks;
        TrackReloader.ReloadAll(null);
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