using System.Collections.Generic;
using System.Threading.Tasks;
using BaboonAPI.Hooks.Tracks;
using BaboonAPI.Hooks.Tracks.Collections;
using BaboonAPI.Utility;
using Microsoft.FSharp.Core;
using UnityEngine;

namespace APTromboneChampMod;

public class TrackCollection : BaseTromboneCollection {
    public TrackCollection() : base("AP", "Archipelago", "All tracks required for Archipelago") {
    }

    public override IEnumerable<TromboneTrack> BuildTrackList() {
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            yield return track;
        }
    }

    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite() {
        throw new System.NotImplementedException();
    }
}

public class TrackCollectionListener : TrackCollectionRegistrationEvent.Listener {
    public IEnumerable<TromboneCollection> OnRegisterCollections() {
        yield return new TrackCollection();
    }
}