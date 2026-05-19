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

            override public void EndTrap(GameController controller) {
                controller.gameplay_settings.mouse_controldirection = GlobalVariables.mousecontrolmode;
            }

            override public float TrapDuration() => 8f;
        }

        public class TrapSilenceTrack() : TrapType(1202L) {
            private float OriginalVolume;

            override public void StartTrap(GameController controller) {
                OriginalVolume = controller.musictrack.volume;
            }

            override public void ContinueTrap(GameController controller) {
                float trackTime = (float)controller.musictrack.timeSamples / (float)controller.musictrack.clip.frequency;
                float trapProgress = (trackTime - TrapStartTime) / (TrapEndTime - TrapStartTime);
                float edgeDist = trapProgress * 2f;
                if (edgeDist > 1f) edgeDist = 2f - edgeDist;

                float vol = 0f;
                if (edgeDist < .25f) vol = 1f - edgeDist / .25f;
                controller.musictrack.volume = vol;
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
            override public void ContinueTrap(GameController controller) {
                controller.noteholderr.anchoredPosition3D = new Vector3(0f, 100f, 0f);
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
                    controller.setPuppetShake(false);
                    controller.setPuppetBreath(true);
                    controller.stopNote();
                }
            }

            override public void EndTrap(GameController controller) {
                controller.breathcounter = 0f;
                controller.outofbreath = false;
                controller.sfxrefs.outofbreath.Stop();
                controller.setPuppetBreath(false);
            }

            override public float TrapDuration() => 2f;
        }

        public class TrapWarbleTrombone() : TrapType(1206L) {
            private const float NUM_CYCLES = 5;
            override public void ContinueTrap(GameController controller) {
                float trackTime = (float)controller.musictrack.timeSamples / (float)controller.musictrack.clip.frequency;
                float trapProgress = (trackTime - TrapStartTime) / (TrapEndTime - TrapStartTime);
                controller.currentnotesound.pitch *= Mathf.Sin(trapProgress * NUM_CYCLES * Mathf.PI * 2) * .2f + .9f;
            }
        }

        public class TrapWarpSpeed() : TrapType(1207L) {
            private static readonly AnimationCurve SpeedCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.1f, 1f),
                new Keyframe(0.3f, 0.9f),
                new Keyframe(0.35f, 0.9f),
                new Keyframe(0.65f, 1.25f),
                new Keyframe(0.7f, 1.25f),
                new Keyframe(0.9f, 1f),
                new Keyframe(1f, 1f)
            );

            override public void ContinueTrap(GameController controller) {
                float trackTime = (float)controller.musictrack.timeSamples / (float)controller.musictrack.clip.frequency;
                float trapProgress = (trackTime - TrapStartTime) / (TrapEndTime - TrapStartTime);

                float warp = SpeedCurve.Evaluate(trapProgress);

                if (GlobalVariables.turbomode) warp *= 2f;
                controller.musictrack.pitch = warp;
                controller.smooth_scrolling_move_mult = warp;
            }

            override public void EndTrap(GameController controller) {
                float warp = GlobalVariables.turbomode ? 2 : 1;
                controller.musictrack.pitch = warp;
                controller.smooth_scrolling_move_mult = warp;
            }

            override public float TrapDuration() => 10f;
        }

        public static readonly TrapType FlipControls = new TrapFlipControls();
        public static readonly TrapType SilenceTrack = new TrapSilenceTrack();
        public static readonly TrapType SilenceTrombone = new TrapSilenceTrombone();
        public static readonly TrapType HideNotes = new TrapHideNotes();
        public static readonly TrapType NoBreath = new TrapNoBreath();

        public static readonly TrapType[] Traps = [
            FlipControls, SilenceTrack, SilenceTrombone, HideNotes, NoBreath,
            new TrapWarbleTrombone(), new TrapWarpSpeed()
        ];

        public readonly long ID = ID;

        virtual public float TrapDuration() => 5f;
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
    private static float TrapStartTime, TrapEndTime;
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
            float expectedEnd = trackTime + TrapQueue[0].TrapDuration();
            if (expectedEnd > controller.levelendtime - 3f) return;
            CurTrap = TrapQueue[0];
            CurTrap.StartTrap(controller);
            TrapQueue.RemoveAt(0);
            TrapStartTime = trackTime;
            TrapEndTime = expectedEnd;
        }

        // deal with ongoing effects
        CurTrap?.ContinueTrap(controller);
    }
}