using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using BaboonAPI.Hooks;
using BaboonAPI.Hooks.Tracks;
using BaboonAPI.Hooks.Tracks.Collections;
using Newtonsoft.Json.Linq;

namespace APTromboneChampMod;

public static class APHandler {
    public static APSettings WorldSettings = new();
    public static Track[] FilteredTracks = []; // all tracks in the apworld based on settings
    public static Track[] AvailableTracks = []; // tracks that can be played due to received items
    public static long[] BeatenTracks = []; // tracks listed as beaten in AP storage
    public static Track? GoalTrack = null;

    public static ArchipelagoSession APSession = null;
    public static int APTeam = -1, APSlot = -1;
    public static readonly Version APVersion = new(0, 6, 7);

    public static readonly List<long> APSentLocations = [];
    public static Hint[] APReceivedHints = [];

    public static Track? FindTrack(string name) {
        Track track = FilteredTracks.FirstOrDefault(track => track.Name == name);
        if (track.Name != name) return null;
        return track;
    }
    
    public static bool IsTrackAvailable(Track track) {
        if (APSession is null || APSlot == -1) return false;
        
        if (!FilteredTracks.Contains(track)) return false;
        if (WorldSettings.TrackGating && !ItemHandler.HasTrack(track.ID)) return false;
        if (track.Difficulty > WorldSettings.MinDiff) {
            if (WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
                if (!ItemHandler.HasDifficulty(track.Difficulty)) return false;
            }
            if (WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
                int diff = track.Difficulty - WorldSettings.MinDiff;
                if (diff > ItemHandler.ProgressiveDifficulties) return false;
            }
        }
        if (track.Name == WorldSettings.GoalTrack) {
            int hotDogs = ItemHandler.HotDogs;
            if (hotDogs < WorldSettings.HotDogs) return false;
        }
        return true;
    }

    private static readonly string[] RatingToString = ["C", "B", "A", "S"];

    public static string GetRatingString(int rating) {
        if (rating < 0) rating = 0;
        if (rating >= RatingToString.Length) rating = RatingToString.Length - 1;
        return RatingToString[rating];
    }

    public static int GetRequiredRating() {
        // 0=C 1=B 2=A 3=S
        int initial = WorldSettings.InitialRating;
        initial -= ItemHandler.RankReductions;
        return initial;
    }

    public static void SendTrack(Track track, bool beaten) {
        if (APSession is null || APSlot == -1) return;
        if (!IsTrackAvailable(track)) return; // precaution
        long[] IDs = beaten ? [track.ID, track.ID + 1000L] : [track.ID];
        APSession.Locations.CompleteLocationChecksAsync(IDs);

        if (!beaten || BeatenTracks.Contains(track.ID)) return;
        APSession.DataStorage["beaten"] += new long[] { track.ID };
        BeatenTracks = [..BeatenTracks, track.ID];
        if (HasGoaled()) {
            APSession.SetGoalAchieved();
            if (APSession.Players.ActivePlayer.Alias != "DONE") APSession.Say("!alias DONE");
        }
    }

    public static bool CanHint() {
        if (APSession is null || APSlot == -1) return false;
        return APSession.RoomState.HintPoints >= APSession.RoomState.HintCost;
    }

    public static string FormatItemHint(Hint hint) {
        if (hint == null) return "-";
        if (hint.ReceivingPlayer == APSlot) {
            // own world, just return the name
            return APSession.Items.GetItemName(hint.ItemId);
        }
        PlayerInfo player = APSession.Players.GetPlayerInfo(hint.ReceivingPlayer);
        string gameName = player.Game;
        string itemName = APSession.Items.GetItemName(hint.ItemId, gameName);
        return $"{player.Alias}'s {itemName} ({hint.Status})";
    }

    public static string FormatLocationString(Hint hint) {
        if (hint == null) return "-";
        if (hint.FindingPlayer == APSlot) {
            // own world, just return the track
            return APSession.Locations.GetLocationNameFromId(hint.LocationId);
        }
        PlayerInfo player = APSession.Players.GetPlayerInfo(hint.FindingPlayer);
        string gameName = player.Game;
        string locName = APSession.Locations.GetLocationNameFromId(hint.LocationId, gameName);
        if (hint.Entrance != "") return $"{player.Alias}'s {locName} ({hint.Entrance})";
        return $"{player.Alias}'s {locName}";
    }

    public static string FormatFullHint(Hint hint) {
        if (hint == null) return "-"; // should never happen
        
        PlayerInfo finder = APSession.Players.GetPlayerInfo(hint.FindingPlayer);
        PlayerInfo receiver = APSession.Players.GetPlayerInfo(hint.ReceivingPlayer);
        string location = APSession.Locations.GetLocationNameFromId(hint.LocationId, finder.Game);
        string item = APSession.Items.GetItemName(hint.ItemId, receiver.Game);
        bool ownGame = hint.ReceivingPlayer == hint.FindingPlayer;

        string entrance = "";
        if (hint.Entrance != "") entrance = $" ({hint.Entrance})";
        
        if (ownGame) return $"{item} is at {location}"; // simpler string for local items

        string receiverName = $"{receiver.Alias}'s ";
        if (receiver.Slot == APSlot) receiverName = "";
        string finderName = $"{finder.Alias}'s ";
        if (finder.Slot == APSlot) finderName = "";
        string hintStatus = $" ({hint.Status})";
        if (receiver.Slot == APSlot) hintStatus = "";
        
        return $"{receiverName}{item} is at {finderName}{location}{entrance}{hintStatus}";
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
            if (!ItemHandler.HasTrack(track.ID)) {
                Hint hint = null;
                foreach (Hint h in APReceivedHints) {
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
                if (!ItemHandler.HasDifficulty(track.Difficulty)) {
                    Hint hint = null;
                    foreach (Hint h in APReceivedHints) {
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
                int found = ItemHandler.ProgressiveDifficulties;
                if (found < req) {
                    int total = WorldSettings.MaxDiff - WorldSettings.MinDiff;
                    total -= found;
                    Hint[] list = new Hint[total];
                    int idx = 0;
                    foreach (Hint h in APReceivedHints) {
                        if (h.ReceivingPlayer == APSlot && h.ItemId == 1011L) list[idx++] = h;
                    }
                    hints.DifficultyUnlocks = list;
                }
            }
        }

        if (!APSentLocations.Contains(track.ID)) {
            Hint hint = null;
            foreach (Hint h in APReceivedHints) {
                if (h.FindingPlayer == APSlot && h.LocationId == track.ID) {
                    hint = h;
                    break;
                }
            }
            hints.PlayReward = hint;
        }
        if (!APSentLocations.Contains(track.ID + 1000L)) {
            Hint hint = null;
            foreach (Hint h in APReceivedHints) {
                if (h.FindingPlayer == APSlot && h.LocationId == track.ID + 1000L) {
                    hint = h;
                    break;
                }
            }
            hints.BeatReward = hint;
        }

        return hints;
    }

    public static void TryHintTrack(Track track) {
        if (!WorldSettings.TrackGating) return;
        if (!CanHint()) return;
        if (APReceivedHints.Any(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == track.ID)) return;
        string item = APSession.Items.GetItemName(track.ID);
        if (item is not null) APSession.Say($"!hint {item}");
    }

    public static void TryHintDifficulty(int diff) {
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.OFF) return;
        if (!CanHint()) return;
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.ON) {
            if (APReceivedHints.Any(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1010L + diff)) return;
        }
        if (WorldSettings.DifficultyGating == APSettings.DiffGateType.PROG) {
            diff = 1;
            int total = WorldSettings.MaxDiff - WorldSettings.MinDiff - ItemHandler.ProgressiveDifficulties;
            if (total == 0) return;
            int found = APReceivedHints.Count(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1011L);
            if (found >= total) return;
        }
        string item = APSession.Items.GetItemName(1010L + diff);
        if (item is not null) APSession.Say($"!hint {item}");
    }

    public static void TryHintRankReduction() {
        if (WorldSettings.GoalRating == WorldSettings.InitialRating) return;
        if (!CanHint()) return;
        int total = WorldSettings.InitialRating - WorldSettings.GoalRating - ItemHandler.RankReductions;
        if (total == 0) return;
        int found = APReceivedHints.Count(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1001L);
        if (found >= total) return;
        string item = APSession.Items.GetItemName(1001L);
        if (item is not null) APSession.Say($"!hint {item}");
    }

    public static void TryHintHotDog() {
        if (WorldSettings.HotDogs == 0) return;
        if (!CanHint()) return;
        int total = WorldSettings.HotDogs + WorldSettings.ExtraHotDogs;
        int found = ItemHandler.HotDogs;
        if (found >= WorldSettings.HotDogs) return;
        total -= found;
        found = APReceivedHints.Count(hint => hint.ReceivingPlayer == APSlot && hint.ItemId == 1004L);
        if (found >= total) return;
        string item = APSession.Items.GetItemName(1004L);
        if (item is not null) APSession.Say($"!hint {item}");
    }

    public static bool HasGoaled() {
        if (WorldSettings.GoalTracks == 0) {
            if (GoalTrack.HasValue) {
                if (BeatenTracks.Contains(GoalTrack.Value.ID)) return true;
            }
            else ArchipelagoPlugin.Logger.LogWarning("Goal tracks is 0 and no goal track is set!");
            return false;
        }
        
        return BeatenTracks.Length >= WorldSettings.GoalTracks;
    }

    public static long ConnectTime;
    public static void ConnectToAP(string host, int port, string slot, string pass) {
        ArchipelagoPlugin.Logger.LogInfo($"Connecting to {host}:{port}");
        APSession?.Socket.DisconnectAsync();
        APSession = null;
        APSlot = -1;
        APReceivedHints = [];
        ItemHandler.ResetItems();
        APSentLocations.Clear();
        OnWorldSettingsChanged();
        try {
            APSession = ArchipelagoSessionFactory.CreateSession(host, port);
            APSession.Items.ItemReceived += helper => {
                List<long> items = [];
                ItemInfo item;
                while (helper.Any()) {
                    item = helper.PeekItem();
                    // handle traps/filler immediately to avoid clogging item list
                
                    if (item.ItemId == 1003L) { // Fun Fact, rate limit to 1/s
                        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - LastFunFact > 1000L) {
                            LastFunFact = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                            if (UnseenFacts.Count == 0) {
                                UnseenFacts.AddRange(FunFacts);
                                UnseenFacts.Shuffle();
                            }

                            APSession.Say($"FUN FACT: {UnseenFacts[0]}");
                            UnseenFacts.RemoveAt(0);
                        }
                    }
                    else if (item.ItemId == 1002L) {} // Nothing
                    else if (item.ItemId is 1005L or 1006L or 1007L or 1008L or 1009L) { // old trap IDs
                        // dont send in first 10s after connecting to avoid doubled traps from disconnection
                        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - ConnectTime > 10000L) {
                            switch (item.ItemId) {
                                case 1005L:
                                    APTrapController.AddTrap(APTrapController.TrapType.FlipControls);
                                    break;
                                case 1006L:
                                    APTrapController.AddTrap(APTrapController.TrapType.SilenceTrack);
                                    break;
                                case 1007L:
                                    APTrapController.AddTrap(APTrapController.TrapType.SilenceTrombone);
                                    break;
                                case 1008L:
                                    APTrapController.AddTrap(APTrapController.TrapType.HideNotes);
                                    break;
                                case 1009L:
                                    APTrapController.AddTrap(APTrapController.TrapType.NoBreath);
                                    break;
                            }
                        }
                    }
                    else {
                        APTrapController.TrapType trap = APTrapController.TrapType.GetTrap(item.ItemId);
                        if (trap != null) {
                            // dont send in first 10s after connecting to avoid doubled traps from disconnection
                            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - ConnectTime > 10000L)
                                APTrapController.AddTrap(trap);
                        }
                        else items.Add(item.ItemId);
                    }
                    helper.DequeueItem();
                }
                // safe to do this with no lock, the function handles it
                if (items.Count > 0) ItemHandler.OnReceivedItems(items);
            };
            APSession.Locations.CheckedLocationsUpdated += locs => {
                // only add locations with the lock, to prevent issues updating collections
                lock (APSentLocations) {
                    foreach (long loc in locs) {
                        if (!APSentLocations.Contains(loc)) {
                            APSentLocations.Add(loc);
                            foreach (Hint hint in APReceivedHints) {
                                if (hint.FindingPlayer == APSlot && hint.LocationId == loc) {
                                    // remove the hint if it exists for this specific location
                                    APReceivedHints = APReceivedHints.Where(it =>
                                        it.FindingPlayer != APSlot ||
                                        it.LocationId    != loc
                                    ).ToArray();
                                    break;
                                }
                            }
                        }
                    }
                }
                OnTrackAvailabilityChanged();
            };
            APSession.Hints.TrackHints(hints => {
                APReceivedHints = hints.Where(hint => !hint.Found).ToArray();
                OnHintsChanged();
            });
            APSession.Socket.SocketClosed += reason => {
                ArchipelagoPlugin.Logger.LogInfo($"Socket closed: {reason}");
                APSlot = -1;
                APSession = null;
                OnWorldSettingsChanged();
            };
            APSession.MessageLog.OnMessageReceived += message => {
                if (message is HintItemSendLogMessage hintMsg) {
                    if (hintMsg.IsRelatedToActivePlayer) {
                        // need to handle it because TrackHints does not
                        int  sender   = hintMsg.Sender.Slot;
                        int  receiver = hintMsg.Receiver.Slot;
                        bool found    = hintMsg.IsFound;
                        long item     = hintMsg.Item.ItemId;
                        long location = hintMsg.Item.LocationId;
                        bool existing = false;
                        foreach (Hint hint in APReceivedHints) {
                            if (hint.FindingPlayer == sender && hint.ReceivingPlayer == receiver && hint.LocationId == location) {
                                existing = true;
                                // remove the hint if the item was found
                                if (found) {
                                    APReceivedHints = APReceivedHints.Where(it =>
                                        it.FindingPlayer   != sender   ||
                                        it.ReceivingPlayer != receiver ||
                                        it.LocationId      != location
                                    ).ToArray();
                                }

                                OnHintsChanged();
                                break;
                            }
                        }

                        if (!found && !existing) {
                            // add the hint if item was not found and the item is new
                            APReceivedHints = [
                                ..APReceivedHints,
                                new Hint {
                                    Entrance        = "",
                                    FindingPlayer   = sender,
                                    Found           = false,
                                    ItemFlags       = hintMsg.Item.Flags,
                                    ItemId          = item,
                                    LocationId      = location,
                                    ReceivingPlayer = receiver,
                                    Status          = (hintMsg.Item.Flags & ItemFlags.Trap) != 0 ? HintStatus.Avoid : (hintMsg.Item.Flags == 0 ? HintStatus.NoPriority : HintStatus.Priority)
                                }
                            ];
                            OnHintsChanged();
                        }
                        if (ArchipelagoPlugin.SendChatToLog) ArchipelagoPlugin.Logger.LogInfo(message.ToString());
                    }
                    return; // for some reason this message type is also ItemSendLogMessage???
                }

                if (message is ItemSendLogMessage itemMsg) {
                    if (itemMsg.IsRelatedToActivePlayer) {
                        // need to handle this to remove hints for own items/locations
                        int  sender   = itemMsg.Sender.Slot;
                        int  receiver = itemMsg.Receiver.Slot;
                        long location = itemMsg.Item.LocationId;
                        foreach (Hint hint in APReceivedHints) {
                            if (hint.FindingPlayer == sender && hint.ReceivingPlayer == receiver && hint.LocationId == location) {
                                // remove the hint
                                APReceivedHints = APReceivedHints.Where(it =>
                                    it.FindingPlayer   != sender   ||
                                    it.ReceivingPlayer != receiver ||
                                    it.LocationId      != location
                                ).ToArray();
                                OnHintsChanged();
                                break;
                            }
                        }
                        if (ArchipelagoPlugin.SendChatToLog) ArchipelagoPlugin.Logger.LogInfo(message.ToString());
                    }
                    return;
                }
                
                if (ArchipelagoPlugin.SendChatToLog) ArchipelagoPlugin.Logger.LogInfo(message.ToString());
            };
            ConnectTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            LoginResult result = APSession.TryConnectAndLogin( // TODO: async version
                "Trombone Champ", slot,
                ItemsHandlingFlags.AllItems,
                APVersion, [], null, pass, true
            );
            ConnectTime = LastFunFact = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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

            APSession.DataStorage["beaten"].Initialize(new long[] { });
            APSession.DataStorage["beaten"].OnValueChanged += (oldData, newData) => {
                BeatenTracks = newData.To<long[]>();
                if (HasGoaled()) APSession.SetGoalAchieved();
            };
            BeatenTracks = APSession.DataStorage["beaten"].To<long[]>();
            
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
            ArchipelagoPlugin.Logger.LogInfo($"Hot Dogs: {WorldSettings.HotDogs} + {WorldSettings.ExtraHotDogs}");
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

            APSession.Hints.GetHintsAsync().ContinueWith(task => {
                if (!task.IsCompletedSuccessfully) return;
                APReceivedHints = task.Result.Where(hint => !hint.Found).ToArray();
                OnHintsChanged();
            });
        }
        catch (Exception e) {
            ArchipelagoPlugin.Logger.LogError($"Unusual error: {e.Message}");
            ArchipelagoPlugin.Logger.LogError(e.StackTrace);
        }
    }

    private static long LastFunFact;

    private static readonly string[] FunFacts = [
        "It takes one-thousand workers a full year to produce a single trombone.",
        "The trombone is related to the trumpet (they are cousins).",
        "The trombone is not related to the French Horn (they are just friends).",
        "Some claim that Mozart's last words before dying were \"At least I got to use a trombone.\"",
        "A student's trombone generally costs between $100 and $300, but a professional trombone can cost over two billion dollars.",
        "To this day, scientists don't really know how a trombone makes sound.",
        "A professional trombone player is known as a \"tromboner\".",
        "Thirty-four countries have outlawed the use of the trombone. In six countries, playing trombone is punishable by death.",
        "Trombones contain \"spit valves\" that allow you to blow gobs of your nasty spit all over the floor.",
        "Without trombones, there could never have been \"ska\" music. Draw your own conclusions from this factoid.",
        "The average baboon can live to be over 300 years old.",
        "There are more baboons on Earth than humans.",
        "Prehistoric trombones were forty feet long and could weigh over six hundred pounds.",
        "Trombones do not float in water, so do not accidentally drop your trombone into the river last week.",
        "Cows love the sound of a trombone (because they are smart).",
        "Playing trombone in your apartment is a great way to make friends with your neighbors.",
        "Despite its name, the trombone does not have any bones.",
        "There are between 2 and 4 spiders living inside the average trombone.",
        "The first trombone was invented in 20,000,000 B.C.",
        "If you placed all of the trombones on Earth end-to-end, they would wrap around the solar system 4 times.",
        "There are more trombones on Earth than there are humans.",
        "The highest note playable on trombones is so high-pitched that only certain species of bats can hear it.",
        "The world record for \"Most Trombones Owned\" is held by Mike Brass of Omaha, Nebraska. He owns two trombones.",
        "It takes over three thousand tons of brass to produce a single trombone.",
        "In real life, there are over nine songs that feature a trombone.",
        "In England, \"trombone\" is spelled \"troumboune\"."
    ];

    private static List<string> UnseenFacts = [];

    public static void OnHintsChanged() {
        LevelSelectController controller = UnityEngine.Object.FindObjectOfType<LevelSelectController>();
        if (controller) controller.populateSongNames(false);
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

        if (ArchipelagoPlugin.Instance.curGUI == 0 && APSlot != -1) ArchipelagoPlugin.Instance.curGUI = 1;
        if (ArchipelagoPlugin.Instance.curGUI == 1 && APSlot == -1) ArchipelagoPlugin.Instance.curGUI = 0;
    }
    
    private static object TrackUpdateLock = new();
    public static void OnTrackAvailabilityChanged() {
        // make absolutely sure that nothing will change during updating
        lock (TrackUpdateLock) {
            lock (APSentLocations) {
                // called when receiving items that might change what tracks are playable
                AvailableTracks = FilteredTracks.Where(IsTrackAvailable).ToArray();

                if (GlobalVariables.chosen_collection_index < 0 || GlobalVariables.chosen_collection_index >= GlobalVariables.all_track_collections.Count) return; // never loaded collections
                    
                // check if the current collection is an AP one
                global::TrackCollection current = GlobalVariables.all_track_collections[GlobalVariables.chosen_collection_index];

                if (TrackCollectionListener.COLLECTIONS.TryGetValue(current._unique_id, out BaseTromboneCollection thisCollection)) {
                    // rebuild the collection manually so the track list actually updates
                    List<TromboneTrack> tracks = thisCollection.BuildTrackList().ToList();
                    global::TrackCollection allCollection = GlobalVariables.all_track_collections.First(coll => coll._unique_id == "all"); // from base game, contains every track
                    current.all_tracks = tracks.Select(track => {
                        return allCollection.all_tracks.First(data => data.trackname_short == track.trackname_short);
                    }).ToList();
                    current._trackcount = tracks.Count;
                    
                    LevelSelectController controller = UnityEngine.Object.FindObjectOfType<LevelSelectController>();
                    if (controller) {
                        // get the currently selected song in the list
                        string name = controller.alltrackslist[controller.songindex].trackname_short;
                        
                        // rebuild the controller's collection, with skipped sort, then do the sort with no animation
                        controller.selectNewCollection(true);
                        controller.sortTracks(GlobalVariables.sortmode, false);

                        // try and select the track that was previously selected
                        int idx = -1;
                        for (int a = 0; a < controller.alltrackslist.Count; a++) {
                            if (controller.alltrackslist[a].trackname_short == name) {
                                idx = a;
                                break;
                            }
                        }

                        if (idx != -1) {
                            // only repopulate names
                            controller.songindex = idx;
                            GlobalVariables.levelselect_index = idx;
                            controller.populateSongNames(false);
                        }
                        else {
                            // brief animations and triggers track preview to update
                            controller.populateSongNames(true);
                        }
                    }
                }

                // wait for this to finish
                var iter = TrackReloader.ReloadAll(null).ForEach(_ => {});
                while (iter.MoveNext()) {}
            }
        }
    }
}