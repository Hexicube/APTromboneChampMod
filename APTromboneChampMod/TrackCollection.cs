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
        ArrayList unseen = new ArrayList(ArchipelagoPlugin.FilteredTracks);
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

    private const string ICON_B64 = "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";
    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite() {
        byte[] imageBytes = Convert.FromBase64String(ICON_B64);
        Texture2D tex = new Texture2D(200, 200);
        tex.LoadImage(imageBytes);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 200f, 200f), new Vector2(0.5f, 0.5f), 100.0f);
        return Coroutines.sync(FuncConvert.FromFunc(() => FSharpResult<Sprite, string>.NewOk(sprite)));
    }
}

public class TrackCollectionListener() : TrackCollectionRegistrationEvent.Listener {
    public IEnumerable<TromboneCollection> OnRegisterCollections() {
        yield return new TrackCollection();
    }
}