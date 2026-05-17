using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaboonAPI.Hooks.Tracks;
using BaboonAPI.Hooks.Tracks.Collections;
using BaboonAPI.Utility;
using Microsoft.FSharp.Core;
using UnityEngine;

namespace APTromboneChampMod;

public interface APCollection {
    public bool HasNoTracks();
    public string GetNoTrackString();
}

public class TrackCollectionAllAP() : BaseTromboneCollection("AP", "Archipelago", "All tracks required for Archipelago"), APCollection {
    public override IEnumerable<TromboneTrack> BuildTrackList() {
        int yielded = 0;
        ArrayList unseen = new ArrayList(APHandler.FilteredTracks);
        List<string> unknownTracks = [];
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            Track? match = null;
            foreach (Track trackDef in unseen) {
                if (trackDef.Name == track.trackname_short) {
                    match = trackDef;
                    break;
                }
            }
            if (match != null) {
                unseen.Remove(match);
                yielded++;
                yield return track;
            }
            else unknownTracks.Add(track.trackname_short);
        }

        if (unseen.Count > 0) {
            // TODO: notify player
            foreach (string missed in unseen) ArchipelagoPlugin.Logger.LogInfo($"Missed track: {missed}");
            foreach (string unknown in unknownTracks) ArchipelagoPlugin.Logger.LogInfo($"Unknown track: {unknown}");
        }

        if (yielded == 0) yield return TrackLookup.allTracks().First(track => track.trackname_short == "Warm-Up"); // prevents issues
    }

    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite() {
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(ImageHandler.ArchipelagoCollection)));
    }

    public bool HasNoTracks() {
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            foreach (Track trackDef in APHandler.FilteredTracks) {
                if (trackDef.Name == track.trackname_short) return false;
            }
        }
        return true;
    }

    public string GetNoTrackString() => "AP song list is empty!\nReport this with your config included.";
}

public class TrackCollectionAvailWithChecksOnly() : BaseTromboneCollection("AP_checks", "Archipelago Checks", "Tracks that are unlocked and have checks remaining"), APCollection {
    public override IEnumerable<TromboneTrack> BuildTrackList() {
        int yielded = 0;
        ArrayList unseen = new ArrayList(APHandler.FilteredTracks);
        List<string> unknownTracks = [];
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            Track? match = null;
            foreach (Track trackDef in unseen) {
                if (trackDef.Name == track.trackname_short) {
                    match = trackDef;
                    break;
                }
            }
            if (match != null) {
                unseen.Remove(match.Value);
                if (APHandler.IsTrackAvailable(match.Value)) {
                    if (
                        !APHandler.APSentLocations.Contains(match.Value.ID) ||
                        !APHandler.APSentLocations.Contains(match.Value.ID + 1000L)
                    ) {
                        yielded++;
                        yield return track;
                    }
                }
            }
            else unknownTracks.Add(track.trackname_short);
        }

        if (unseen.Count > 0) {
            // TODO: notify player
            foreach (string missed in unseen) ArchipelagoPlugin.Logger.LogInfo($"Missed track: {missed}");
            foreach (string unknown in unknownTracks) ArchipelagoPlugin.Logger.LogInfo($"Unknown track: {unknown}");
        }

        if (yielded == 0) yield return TrackLookup.allTracks().First(track => track.trackname_short == "Warm-Up"); // prevents issues
    }

    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite() {
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(ImageHandler.ArchipelagoCollectionFiltered)));
    }

    public bool HasNoTracks() {
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            foreach (Track trackDef in APHandler.FilteredTracks) {
                if (trackDef.Name == track.trackname_short) {
                    if (APHandler.IsTrackAvailable(trackDef)) {
                        if (
                            !APHandler.APSentLocations.Contains(trackDef.ID) ||
                            !APHandler.APSentLocations.Contains(trackDef.ID + 1000L)
                        ) return false;
                    }
                }
            }
        }
        return true;
    }

    public string GetNoTrackString() => "No songs left to play.\nMaybe press F2 to hint something?";
}

public class TrackCollectionLockedOnly() : BaseTromboneCollection("AP_locked", "Archipelago Locked", "Tracks that aren't yet available"), APCollection {
    public override IEnumerable<TromboneTrack> BuildTrackList() {
        int yielded = 0;
        ArrayList unseen = new ArrayList(APHandler.FilteredTracks);
        List<string> unknownTracks = [];
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            Track? match = null;
            foreach (Track trackDef in unseen) {
                if (trackDef.Name == track.trackname_short) {
                    match = trackDef;
                    break;
                }
            }
            if (match != null) {
                unseen.Remove(match.Value);
                if (!APHandler.IsTrackAvailable(match.Value)) {
                    yielded++;
                    yield return track;
                }
            }
            else unknownTracks.Add(track.trackname_short);
        }

        if (unseen.Count > 0) {
            // TODO: notify player
            foreach (string missed in unseen) ArchipelagoPlugin.Logger.LogInfo($"Missed track: {missed}");
            foreach (string unknown in unknownTracks) ArchipelagoPlugin.Logger.LogInfo($"Unknown track: {unknown}");
        }

        if (yielded == 0) yield return TrackLookup.allTracks().First(track => track.trackname_short == "Warm-Up"); // prevents issues
    }

    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite() {
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(ImageHandler.ArchipelagoCollectionLocked)));
    }

    public bool HasNoTracks() {
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            foreach (Track trackDef in APHandler.FilteredTracks) {
                if (trackDef.Name == track.trackname_short) {
                    if (!APHandler.IsTrackAvailable(trackDef)) return false;
                }
            }
        }
        return true;
    }

    public string GetNoTrackString() => "All songs unlocked.\nGo play them!";
}

public class TrackCollectionListener : TrackCollectionRegistrationEvent.Listener {
    public static readonly Dictionary<string, BaseTromboneCollection> COLLECTIONS = new() {
        {"AP", new TrackCollectionAllAP()},
        {"AP_checks", new TrackCollectionAvailWithChecksOnly()},
        {"AP_locked", new TrackCollectionLockedOnly()}
    };
    
    public IEnumerable<TromboneCollection> OnRegisterCollections() {
        foreach (BaseTromboneCollection collection in COLLECTIONS.Values) yield return collection;
    }
}