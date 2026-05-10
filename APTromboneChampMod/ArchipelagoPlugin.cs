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
    
    private void Awake() {
        Instance = this;
        FilteredTracks = APTracks.GetTrackList(WorldSettings).ToArray();
        
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is running at version {MyPluginInfo.PLUGIN_VERSION}");
        Logger.LogInfo($"Hello World!");
        GameInitializationEvent.Register(Info, TryInitialize);
    }

    private void TryInitialize() {
        TrackCollectionRegistrationEvent.EVENT.Register(new TrackCollectionListener());
        TrackReloader.ReloadAll(null);
    }
}