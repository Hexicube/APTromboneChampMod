namespace APTromboneChampMod;

public struct APSettings {
    public enum DiffGateType {
        OFF = 0,
        ON = 1,
        PROG = 2
    }
    
    public int GoalTracks = 1;
    public string GoalTrack = null;
    public int GoalRating = 3;
    public int InitialRating = 3;
    public int EasyTrackGap = 0;
    public int HotDogs = 0;
    public int ExtraHotDogs = 0;
    public bool TrackGating = false;
    public DiffGateType DifficultyGating = DiffGateType.OFF;

    public int MinDiff = 1;
    public int MaxDiff = 10;
    public bool Unsafe = true;
    public bool Celeste = true;
    public bool PizzaTower = true;
    public bool UndertaleDeltarune = true;
    public string[] RemovedTracks = [];

    public APSettings() {}
}