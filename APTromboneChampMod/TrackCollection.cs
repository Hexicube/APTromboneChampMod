using System;
using System.Collections;
using System.Collections.Generic;
using BaboonAPI.Hooks.Tracks;
using BaboonAPI.Hooks.Tracks.Collections;
using BaboonAPI.Utility;
using Microsoft.FSharp.Core;
using UnityEngine;

namespace APTromboneChampMod;

public class TrackCollection() : BaseTromboneCollection("AP", "Archipelago", "All tracks required for Archipelago") {
    public override IEnumerable<TromboneTrack> BuildTrackList() {
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
                yield return track;
            }
            else unknownTracks.Add(track.trackname_short);
        }

        if (unseen.Count > 0) {
            // TODO: notify player
            foreach (string missed in unseen) ArchipelagoPlugin.Logger.LogInfo($"Missed track: {missed}");
            foreach (string unknown in unknownTracks) ArchipelagoPlugin.Logger.LogInfo($"Unknown track: {unknown}");
        }
    }

    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite() {
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(Base64Images.Archipelago)));
    }
}

public class TrackCollectionListener() : TrackCollectionRegistrationEvent.Listener {
    public IEnumerable<TromboneCollection> OnRegisterCollections() {
        yield return new TrackCollection();
    }
}