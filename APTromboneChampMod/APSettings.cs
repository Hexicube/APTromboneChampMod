namespace APTromboneChampMod;

public struct APSettings {
    public enum DiffGateType {
        OFF = 0,
        ON = 1,
        PROG = 2
    }
    
    public int GoalTracks;
    public Track? GoalTrack;
    public int GoalRating;
    public int InitialRating;
    public int EasyTrackGap;
    public int HotDogs;
    public int ExtraHotDogs;
    public bool TrackGating;
    public DiffGateType DifficultyGating;

    public int MinDiff;
    public int MaxDiff;
    public bool Unsafe;
    public bool Celeste;
    public bool PizzaTower;
    public bool UndertaleDeltarune;
    public Track[] RemovedTracks;
}