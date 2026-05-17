using System.Collections.Generic;
using UnityEngine;

namespace APTromboneChampMod;

public static class APTrapController {
    public enum TrapType {
        NONE,
        FlipControls,
        SilenceTrack,
        SilenceTrombone,
        HideNotes,
        NoBreath
    }

    public static List<TrapType> TrapQueue = [];

    public static void AddTrap(TrapType t) {
        ArchipelagoPlugin.Logger.LogInfo($"Added trap: {t}");
        TrapQueue.Add(t);
    }

    private static TrapType CurTrap = TrapType.NONE;
    private static float TrapEndTime;
    private static float CorrectTrackVolume;
    public static void ControllerUpdate(GameController controller) {
        float trackTime = (float)controller.musictrack.timeSamples / (float)controller.musictrack.clip.frequency;
        
        // try to end an existing trap
        if (CurTrap != TrapType.NONE && trackTime >= TrapEndTime) {
            switch (CurTrap) {
                case TrapType.FlipControls:
                    controller.gameplay_settings.mouse_controldirection = GlobalVariables.mousecontrolmode;
                    break;
                case TrapType.SilenceTrack:
                    controller.musictrack.volume = CorrectTrackVolume;
                    break;
                case TrapType.HideNotes:
                    controller.zeroxpos = 60f;
                    break;
            }
            ArchipelagoPlugin.Logger.LogInfo($"Ended trap: {CurTrap}");
            
            CurTrap = TrapType.NONE;
            return;
        }
        
        // try to start a new trap
        if (CurTrap == TrapType.NONE) {
            if (TrapQueue.Count == 0) return;
            float earliestAllowed = TrapEndTime + 3f;
            if (trackTime < earliestAllowed) return;
            float expectedEnd = trackTime + 5f;
            if (expectedEnd > controller.levelendtime) return;
            CurTrap = TrapQueue[0];
            ArchipelagoPlugin.Logger.LogInfo($"Started trap: {CurTrap}");
            TrapQueue.RemoveAt(0);
            TrapEndTime = expectedEnd;

            switch (CurTrap) {
                case TrapType.FlipControls:
                    int style = GlobalVariables.mousecontrolmode;
                    switch (style) {
                        case 0: style = 1; break;
                        case 1: style = 0; break;
                        case 2: style = 3; break;
                        case 3: style = 2; break;
                    }
                    controller.gameplay_settings.mouse_controldirection = style;
                    break;
                case TrapType.SilenceTrack:
                    CorrectTrackVolume = controller.musictrack.volume;
                    controller.musictrack.volume = 0f;
                    break;
                case TrapType.HideNotes:
                    controller.zeroxpos = 100000f;
                    break;
                case TrapType.NoBreath:
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
                    break;
            }
        }
        
        // deal with ongoing effects
        if (CurTrap == TrapType.SilenceTrombone) controller.currentnotesound.volume = 0f;
    }
}