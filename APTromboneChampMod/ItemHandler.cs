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
        public static void OnReceivedItems(List<long> items) {
            // can sometimes be empty (received a filler/trap)
            if (items.Count == 0) return;

            // safely add items to a queue
            lock (ItemQueue) { ItemQueue.AddRange(items); }
        }

        private static void Run(object state) {
            if (!Monitor.TryEnter(timer)) return; // already running, skip this invocation

            try {
                bool updateTracks = false;
                bool updateHints = false;

                long[] items;
                lock (APHandler.APFoundItems) {
                    lock (ItemQueue) {
                        items = ItemQueue.ToArray();
                        APHandler.APFoundItems.AddRange(ItemQueue);
                        ItemQueue.Clear();
                    }
                }

                foreach (long item in items) {
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
