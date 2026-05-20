using System;
using System.Collections.Generic;
using System.Threading;

namespace APTromboneChampMod {
    public static class ItemHandler {
        private static Timer timer; // must have a reference to prevent GC
        static ItemHandler() {
            timer = new Timer(Run, null, 0, 100);
        }

        private static List<long> ItemQueue = [];

        private static readonly List<long> ReceivedTracks = [];

        public static bool HasTrack(long ID) {
            // thread-safe no-blocking check
            try {
                for (int a = 0; a < ReceivedTracks.Count; a++) {
                    if (ReceivedTracks[a] == ID) return true;
                }
                return false;
            }
            catch (IndexOutOfRangeException) {
                // array changed during the check and got smaller
                return false;
            }
        }

        public static int RankReductions { get; private set; }
        public static int HotDogs        { get; private set; }
        
        public static int  ProgressiveDifficulties { get; private set; }
        private static bool HasDifficulty2;
        private static bool HasDifficulty3;
        private static bool HasDifficulty4;
        private static bool HasDifficulty5;
        private static bool HasDifficulty6;
        private static bool HasDifficulty7;
        private static bool HasDifficulty8;
        private static bool HasDifficulty9;
        private static bool HasDifficulty10;

        public static bool HasDifficulty(int diff) {
            // helper function
            switch (diff) {
                case 2: return HasDifficulty2;
                case 3: return HasDifficulty3;
                case 4: return HasDifficulty4;
                case 5: return HasDifficulty5;
                case 6: return HasDifficulty6;
                case 7: return HasDifficulty7;
                case 8: return HasDifficulty8;
                case 9: return HasDifficulty9;
                case 10: return HasDifficulty10;
                default:
                    ArchipelagoPlugin.Logger.LogWarning($"HasDifficulty called with difficulty {diff}");
                    return false;
            }
        }

        public static void OnReceivedItems(List<long> items) {
            // safely add items to a queue
            lock (ItemQueue) { ItemQueue.AddRange(items); }
        }

        public static void ResetItems() {
            // reset everything for new AP connection
            lock (timer) {
                ReceivedTracks.Clear();
                RankReductions          = 0;
                HotDogs                 = 0;
                ProgressiveDifficulties = 0;
                HasDifficulty2          = false;
                HasDifficulty3          = false;
                HasDifficulty4          = false;
                HasDifficulty5          = false;
                HasDifficulty6          = false;
                HasDifficulty7          = false;
                HasDifficulty8          = false;
                HasDifficulty9          = false;
                HasDifficulty10         = false;
            }
        }

        private static void Run(object state) {
            if (!Monitor.TryEnter(timer)) return; // already running, skip this invocation

            try {
                bool updateTracks = false;
                bool updateHints = false;

                long[] items;
                lock (ItemQueue) {
                    items = ItemQueue.ToArray();
                    ItemQueue.Clear();
                }

                foreach (long item in items) {
                    if (item < 1000L) ReceivedTracks.Add(item);
                    else if (item == 1001L) RankReductions++;
                    else if (item == 1004L) HotDogs++;
                    else if (item == 1011L) ProgressiveDifficulties++;
                    else if (item == 1012L) HasDifficulty2 = true;
                    else if (item == 1013L) HasDifficulty3 = true;
                    else if (item == 1014L) HasDifficulty4 = true;
                    else if (item == 1015L) HasDifficulty5 = true;
                    else if (item == 1016L) HasDifficulty6 = true;
                    else if (item == 1017L) HasDifficulty7 = true;
                    else if (item == 1018L) HasDifficulty8 = true;
                    else if (item == 1019L) HasDifficulty9 = true;
                    else if (item == 1020L) HasDifficulty10 = true;
                
                    // TODO: handle all the items
                    
                    if ((item > 0L && item < 1000L) || item is 1001L or 1004L || item > 1010L) updateTracks = true;
                    if (item is 1001L or 1004L or 1011L || item > 1011L) updateHints = true;
                }

                if (updateTracks) APHandler.OnTrackAvailabilityChanged();
                else if (updateHints) APHandler.OnHintsChanged();
            }
            catch (Exception e) {
                ArchipelagoPlugin.Logger.LogError(e);
            }
            finally {
                Monitor.Exit(timer);
            }
        }
    }
}
