using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using BaboonAPI.Hooks;
using Newtonsoft.Json.Linq;

namespace APTromboneChampMod;

public static class APHandler {
    public static APSettings WorldSettings = new();
    public static Track[] FilteredTracks = []; // all tracks in the apworld based on settings
    public static Track[] AvailableTracks = []; // tracks that can be played due to received items
    public static Track? GoalTrack = null;

    public static ArchipelagoSession APSession = null;
    public static int APTeam = -1, APSlot = -1;
    public static Version VERSION = new(0, 6, 7);

    public static List<long> ITEMS = [];
    public static List<long> SENT_LOCS = [];
    public static Hint[] HINTS = [];
    
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

    public static int GetRequiredRating() {
        // 0=C 1=B 2=A 3=S
        int initial = WorldSettings.InitialRating;
        initial -= ITEMS.Count(id => id == 1001L);
        return initial;
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
                else ArchipelagoPlugin.Logger.LogWarning("Goal tracks is 0 and no goal track is set!");
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

    public static TrackHints GetTrackHints(Track track) {
        /*
        Returns a struct containing the following information (assuming the relevant items are not found and the hints exist):
        - A hint for the track unlock item, if track gating is on
        - An array for difficulty unlock items:
          - If difficulty gating is off, difficulty gating is not blocking this track, a size 0 array
          - If difficulty gating is on, a size 1 array for the specific difficulty
          - If difficulty gating is progressive, an array sized for how many progressive difficulties remain
        - A hint for the play reward of the track, if not already completed
        - A hint for the beat reward of the track, if not already completed
        Hints may be null to represent a missing item with no associated hint.
        */
        
        TrackHints hints = new();
        if (WorldSettings.TrackGating) {
            if (!ITEMS.Contains(track.ID)) {
                Hint hint = null;
                foreach (Hint h in HINTS) {
                    if (h.ReceivingPlayer == APSlot && h.ItemId == track.ID) {
                        hint = h;
                        break;
                    }
                }
                hints.TrackUnlock = hint;
            }
        }

        if (track.Difficulty > WorldSettings.MinDiff) {
            if (WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
                if (!ITEMS.Contains(1010L + track.Difficulty)) {
                    Hint hint = null;
                    foreach (Hint h in HINTS) {
                        if (h.ReceivingPlayer == APSlot && h.ItemId == 1010L + track.Difficulty) {
                            hint = h;
                            break;
                        }
                    }
                    hints.DifficultyUnlocks = [hint];
                }
            }
            if (WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
                int req = track.Difficulty - WorldSettings.MinDiff;
                int found = ITEMS.Count(id => id == 1011L);
                if (found < req) {
                    int total = WorldSettings.MaxDiff - WorldSettings.MinDiff - 1;
                    total -= found;
                    Hint[] list = new Hint[total];
                    int idx = 0;
                    foreach (Hint h in HINTS) {
                        if (h.ReceivingPlayer == APSlot && h.ItemId == 1011L) list[idx++] = h;
                    }
                    hints.DifficultyUnlocks = list;
                }
            }
        }

        if (!SENT_LOCS.Contains(track.ID)) {
            Hint hint = null;
            foreach (Hint h in HINTS) {
                if (h.FindingPlayer == APSlot && h.LocationId == track.ID) {
                    hint = h;
                    break;
                }
            }
            hints.PlayReward = hint;
        }
        if (!SENT_LOCS.Contains(track.ID + 1000L)) {
            Hint hint = null;
            foreach (Hint h in HINTS) {
                if (h.FindingPlayer == APSlot && h.LocationId == track.ID + 1000L) {
                    hint = h;
                    break;
                }
            }
            hints.BeatReward = hint;
        }

        return hints;
    }

    public static Hint[] GetHotDogHints() {
        /*
        Gets an array containing hints for all remaining hot dogs.
        The array may contain null entries to represent unhinted hot dogs.
        The array size matches the number of missing hot dogs.
        Returns an empty array if enough hot dogs were found to unlock the goal track.
        */
        int total = WorldSettings.HotDogs + WorldSettings.ExtraHotDogs;
        int found = ITEMS.Count(id => id == 1004L);
        if (found >= WorldSettings.HotDogs) return [];
        total -= found;
        Hint[] list = new Hint[total];
        int idx = 0;
        foreach (Hint h in HINTS) {
            if (h.ReceivingPlayer == APSlot && h.ItemId == 1004L) list[idx++] = h;
        }
        return list;
    }

    public static void TryHintTrack(Track track) {
        if (!WorldSettings.TrackGating) return;
        if (!CanHint()) return;
        if (HINTS.Any(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == track.ID)) return;
        string loc = APSession.Locations.GetLocationNameFromId(track.ID);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void TryHintDifficulty(int diff) {
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.OFF) return;
        if (!CanHint()) return;
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
            if (HINTS.Any(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1010L + diff)) return;
        }
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
            diff = 1;
            int total = WorldSettings.MaxDiff - WorldSettings.MinDiff - 1 - ITEMS.Count(id => id == 1011L);
            if (total == 0) return;
            int found = HINTS.Count(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1011L);
            if (found >= total) return;
        }
        string loc = APSession.Locations.GetLocationNameFromId(1010L + diff);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void TryHintRankReduction() {
        if (WorldSettings.GoalRating == WorldSettings.InitialRating) return;
        if (!CanHint()) return;
        int total = WorldSettings.InitialRating - WorldSettings.GoalRating - ITEMS.Count(id => id == 1001L);
        if (total == 0) return;
        int found = HINTS.Count(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1001L);
        if (found >= total) return;
        string loc = APSession.Locations.GetLocationNameFromId(1001L);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void TryHintHotDog() {
        if (WorldSettings.HotDogs == 0) return;
        if (!CanHint()) return;
        int total = WorldSettings.HotDogs + WorldSettings.ExtraHotDogs;
        int found = ITEMS.Count(id => id == 1004L);
        if (found >= WorldSettings.HotDogs) return;
        total -= found;
        found = HINTS.Count(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1004L);
        if (found >= total) return;
        string loc = APSession.Locations.GetLocationNameFromId(1004L);
        if (loc is not null) APSession.Say($"!hint {loc}");
    }

    public static void ConnectToAP(string host, int port, string slot, string pass) {
        ArchipelagoPlugin.Logger.LogInfo($"Connecting to {host}:{port}");
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
            APSession.Hints.TrackHints((hints) => {
                HINTS = hints.Where(hint => !hint.Found).ToArray();
                OnHintsChanged();
            });
            APSession.Socket.SocketClosed += (reason) => {
                ArchipelagoPlugin.Logger.LogInfo($"Socket closed: {reason}");
                APSlot = -1;
                APSession = null;
                OnWorldSettingsChanged();
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
                ArchipelagoPlugin.Logger.LogWarning($"Failed to connect to {host}:{port}");
                LoginFailure failure = (LoginFailure)result;
                foreach (string error in failure.Errors) ArchipelagoPlugin.Logger.LogWarning($"    {error}");
                foreach (ConnectionRefusedError error in failure.ErrorCodes) ArchipelagoPlugin.Logger.LogWarning($"    {error}");
                return;
            }

            LoginSuccessful success = (LoginSuccessful)result;
            APTeam = success.Team;
            APSlot = success.Slot;
            ArchipelagoPlugin.Logger.LogInfo($"Successfully connected to {host}:{port} - Team: {APTeam}, Slot: {APSlot}");
            
            WorldSettings.GoalTracks = int.Parse(success.SlotData["goal"].ToString());
            WorldSettings.GoalTrack = success.SlotData["goal_track"].ToString();
            if (WorldSettings.GoalTracks == 0) ArchipelagoPlugin.Logger.LogInfo($"Goal track: {WorldSettings.GoalTrack}");
            else ArchipelagoPlugin.Logger.LogInfo($"Goal track count: {WorldSettings.GoalTracks}");
            WorldSettings.GoalRating = int.Parse(success.SlotData["rating"].ToString());
            WorldSettings.InitialRating = int.Parse(success.SlotData["rating_start"].ToString());
            ArchipelagoPlugin.Logger.LogInfo($"Goal rating: {WorldSettings.GoalRating} | Initial Rating: {WorldSettings.InitialRating}");
            WorldSettings.EasyTrackGap = int.Parse(success.SlotData["easy_track"].ToString());
            ArchipelagoPlugin.Logger.LogInfo($"Easy Track Gap: {WorldSettings.EasyTrackGap}");
            WorldSettings.HotDogs = int.Parse(success.SlotData["hot_dogs"].ToString());
            WorldSettings.ExtraHotDogs = int.Parse(success.SlotData["extra_hot_dogs"].ToString());
            ArchipelagoPlugin.Logger.LogInfo($"HotDogs: {WorldSettings.HotDogs} + {WorldSettings.ExtraHotDogs}");
            WorldSettings.TrackGating = int.Parse(success.SlotData["track_gating"].ToString()) > 0;
            ArchipelagoPlugin.Logger.LogInfo($"Track gating: {WorldSettings.TrackGating}");
            WorldSettings.DifficultyGating = (APSettings.DiffGateType)int.Parse(success.SlotData["difficulty_gating"].ToString());
            ArchipelagoPlugin.Logger.LogInfo($"Difficulty gating: {WorldSettings.DifficultyGating}");
            WorldSettings.MinDiff = int.Parse(success.SlotData["min_diff"].ToString());
            WorldSettings.MaxDiff = int.Parse(success.SlotData["max_diff"].ToString());
            ArchipelagoPlugin.Logger.LogInfo($"Difficulty range: {WorldSettings.MinDiff} - {WorldSettings.MaxDiff}");
            WorldSettings.Unsafe = int.Parse(success.SlotData["unsafe"].ToString()) != 0;
            ArchipelagoPlugin.Logger.LogInfo($"Unsafe: {WorldSettings.Unsafe}");
            WorldSettings.Celeste = int.Parse(success.SlotData["celeste"].ToString()) != 0;
            ArchipelagoPlugin.Logger.LogInfo($"Celeste DLC: {WorldSettings.Celeste}");
            WorldSettings.PizzaTower = int.Parse(success.SlotData["pizza_tower"].ToString()) != 0;
            ArchipelagoPlugin.Logger.LogInfo($"Pizza Tower DLC: {WorldSettings.PizzaTower}");
            WorldSettings.UndertaleDeltarune = int.Parse(success.SlotData["undertale_deltarune"].ToString()) != 0;
            ArchipelagoPlugin.Logger.LogInfo($"Undertale/Deltarune DLC: {WorldSettings.UndertaleDeltarune}");
            WorldSettings.RemovedTracks = ((JArray)success.SlotData["removed_tracks"]).ToObject<string[]>();
            ArchipelagoPlugin.Logger.LogInfo($"Removed tracks: {string.Join(", ", WorldSettings.RemovedTracks)}");
            OnWorldSettingsChanged();
        }
        catch (Exception e) {
            ArchipelagoPlugin.Logger.LogError($"Unusual error: {e.Message}");
            ArchipelagoPlugin.Logger.LogError(e.StackTrace);
        }
    }
    
    public static void OnReceivedItems(List<long> items) {
        ITEMS.AddRange(items);
        bool updateTracks = false;
        bool updateHints = false;
        bool refreshHints = false;
        foreach (long item in items) {
            if ((item > 0L && item < 1000L) || item is 1001L or 1004L || item > 1010L) updateTracks = true;
            if (item is 1001L or 1004L or 1011L) refreshHints = true;
            if (item > 1011L) updateHints = true;
        }
        if (refreshHints) {
            // there are multiple of these specific items, this tends to break hint tracking
            HINTS = APSession.Hints.GetHints().Where(hint => !hint.Found).ToArray();
            OnHintsChanged();
        }
        else if (updateHints) OnHintsChanged(); // specific difficulty unlocks only
        if (updateTracks) OnTrackAvailabilityChanged();
    }

    public static void OnHintsChanged() {
        // TODO: update hint displays when added
    }

    public static void OnWorldSettingsChanged() {
        // called when connecting to an AP session
        if (APSession is null || APSlot == -1) {
            // force the collection to be empty
            WorldSettings.MinDiff = 2;
            WorldSettings.MaxDiff = 1;
        }
        FilteredTracks = APTracks.GetTrackList(WorldSettings).ToArray();
        GoalTrack = APTracks.GetGoalTrack(WorldSettings, FilteredTracks);
        OnTrackAvailabilityChanged();
        TrackReloader.ReloadAll(null);
    }

    public static void OnTrackAvailabilityChanged() {
        // called when receiving items that might change what tracks are playable
        AvailableTracks = FilteredTracks.Where(IsTrackAvailable).ToArray();
        // TODO: make sure a track cant be played if not available
        // TODO: update hint display specific to currently visible track
    }
}