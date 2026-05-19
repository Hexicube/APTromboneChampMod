using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace APTromboneChampMod;

public class APTrapController {
    public class TrapType (long ID) {
        public class TrapFlipControls() : TrapType(1201L) {
            override public void StartTrap(GameController controller) {
                int style = GlobalVariables.mousecontrolmode;
                switch (style) {
                    case 0: style = 1; break;
                    case 1: style = 0; break;
                    case 2: style = 3; break;
                    case 3: style = 2; break;
                }
                controller.gameplay_settings.mouse_controldirection = style;
            }

            public override void EndTrap(GameController controller) {
                controller.gameplay_settings.mouse_controldirection = GlobalVariables.mousecontrolmode;
            }
        }

        public class TrapSilenceTrack() : TrapType(1202L) {
            private float OriginalVolume;

            override public void StartTrap(GameController controller) {
                OriginalVolume = controller.musictrack.volume;
            }

            override public void ContinueTrap(GameController controller) {
                controller.musictrack.volume = 0f;
            }

            override public void EndTrap(GameController controller) {
                controller.musictrack.volume = OriginalVolume;
            }
        }

        public class TrapSilenceTrombone() : TrapType(1203L) {
            override public void ContinueTrap(GameController controller) {
                controller.currentnotesound.volume = 0f;
            }

            override public void EndTrap(GameController controller) {
                if (controller.noteactive) controller.currentnotesound.volume = 1f;
            }
        }

        public class TrapHideNotes() : TrapType(1204L) {
            // TODO: only hide the notes, not the pitch indicator

            override public void StartTrap(GameController controller) {
                controller.zeroxpos = 100000f;
            }

            override public void EndTrap(GameController controller) {
                controller.zeroxpos = 60f;
            }
        }

        public class TrapNoBreath() : TrapType(1205L) {
            override public void StartTrap(GameController controller) {
                controller.breathcounter = 1f;
                if (!controller.outofbreath) {
                    // copied from Update function
                    controller.sfxrefs.outofbreath.Play();
                    controller.breathglow.anchoredPosition3D = new Vector3(-380f, 0f, 0f);
                    controller.outofbreath = true;
                    controller.noteplaying = false;
                    controller.setPuppetShake(shake: false);
                    controller.setPuppetBreath(hasbreath: true);
                    controller.stopNote();
                }
            }
        }

        public static readonly TrapType FlipControls = new TrapFlipControls();
        public static readonly TrapType SilenceTrack = new TrapSilenceTrack();
        public static readonly TrapType SilenceTrombone = new TrapSilenceTrombone();
        public static readonly TrapType HideNotes = new TrapHideNotes();
        public static readonly TrapType NoBreath        = new(1205L);

        public static readonly TrapType[] Traps = [
            FlipControls, SilenceTrack, SilenceTrombone, HideNotes, NoBreath
        ];

        public readonly long ID = ID;

        virtual public void StartTrap(GameController controller) {}
        virtual public void ContinueTrap(GameController controller) {}
        virtual public void EndTrap(GameController controller) {}

        public static TrapType GetTrap(long ID) => Traps.FirstOrDefault(trap => trap.ID == ID);
    }

    public static List<TrapType> TrapQueue = [];

    public static void AddTrap(TrapType t) => TrapQueue.Add(t);

    public static void ResetState() {
        TrapEndTime = 0f;
        if (CurTrap != null) {
            ArchipelagoPlugin.Logger.LogWarning("Trap controller was reset with active traps!");
            TrapQueue.Insert(0, CurTrap);
        }
        CurTrap = null;
    }

    private static TrapType CurTrap;
    private static float TrapEndTime;
    public static void ControllerUpdate(GameController controller) {
        if (!controller.musictrack || !controller.musictrack.clip) return; // track ended
        
        float trackTime = (float)controller.musictrack.timeSamples / (float)controller.musictrack.clip.frequency;
        
        // try to end an existing trap
        if (CurTrap != null && trackTime >= TrapEndTime) {
            CurTrap.EndTrap(controller);
            CurTrap = null;
            return;
        }
        
        // try to start a new trap
        if (CurTrap == null) {
            if (TrapQueue.Count == 0) return;
            float earliestAllowed = TrapEndTime + 3f;
            if (trackTime < earliestAllowed) return;
            float expectedEnd = trackTime + 5f;
            if (expectedEnd > controller.levelendtime) return;
            CurTrap = TrapQueue[0];
            CurTrap.StartTrap(controller);
            TrapQueue.RemoveAt(0);
            TrapEndTime = expectedEnd;
        }

        // deal with ongoing effects
        CurTrap?.ContinueTrap(controller);
    }
}