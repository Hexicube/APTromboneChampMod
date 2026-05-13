using Archipelago.MultiClient.Net.Models;

namespace APTromboneChampMod;

public struct TrackHints
{
    public TrackHints() {}

    public Hint TrackUnlock = null;
    public Hint[] DifficultyUnlocks = [];
    public Hint PlayReward = null;
    public Hint BeatReward = null;
}