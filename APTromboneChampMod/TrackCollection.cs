using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaboonAPI.Hooks.Tracks;
using BaboonAPI.Hooks.Tracks.Collections;
using BaboonAPI.Utility;
using Microsoft.FSharp.Core;
using UnityEngine;

namespace APTromboneChampMod;

public class TrackCollection() : BaseTromboneCollection("AP", "Archipelago", "All tracks required for Archipelago") {
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
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(Base64Images.ArchipelagoCollection)));
    }
}

public class TrackCollectionAvailWithChecksOnly() : BaseTromboneCollection("AP_checks", "Archipelago Checks", "Tracks that are unlocked and have checks remaining") {
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
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(Base64Images.ArchipelagoCollection)));
    }
}

public class TrackCollectionListener : TrackCollectionRegistrationEvent.Listener {
    public IEnumerable<TromboneCollection> OnRegisterCollections() {
        yield return new TrackCollection();
        yield return new TrackCollectionAvailWithChecksOnly();
    }
}