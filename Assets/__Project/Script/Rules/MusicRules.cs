using System;

public static class MusicRules
{
    public static int NextTrack(MusicMode mode, int current, int count, int randomValue)
    {
        if (count <= 1) return 0;

        switch (mode)
        {
            case MusicMode.Loop:
                return current >= 0 && current < count ? current : 0;
            case MusicMode.Sequence:
                return (current + 1) % count;
            default:
                int pick = Math.Abs(randomValue % (count - 1));
                return pick >= current ? pick + 1 : pick;
        }
    }
}
