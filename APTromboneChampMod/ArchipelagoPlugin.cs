using System.Linq;
using BaboonAPI.Hooks;
using BaboonAPI.Hooks.Initializer;
using BaboonAPI.Hooks.Tracks;
using BepInEx;
using BepInEx.Logging;

namespace APTromboneChampMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ch.offbeatwit.baboonapi.plugin")]
public class ArchipelagoPlugin : BaseUnityPlugin {
    public static ArchipelagoPlugin Instance;
    internal new static ManualLogSource Logger;

    public static APSettings WorldSettings = new();
    public static Track[] FilteredTracks = [];
    public static Track? GoalTrack = null;

    public void OnWorldSettingsChanged() {
        // called when connecting to an AP session
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
        // specific settings to only show Baboons! track, for testing
        WorldSettings.MinDiff = 6;
        WorldSettings.MaxDiff = 6;
        WorldSettings.Unsafe = false;
        WorldSettings.RemovedTracks = [
            "Chop Waltz", "Funiculi Funicula", "Hello! Ma Baby", "Rosamunde", "SkaBIRD", "Skeleton Rag"
        ];
        OnWorldSettingsChanged();
    }
}