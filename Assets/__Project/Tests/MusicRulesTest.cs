using System.Collections.Generic;
using NUnit.Framework;

public class MusicRulesTests
{
    [Test]
    public void NextTrack_EmptyPlaylist_ReturnsZero()
    {
        Assert.AreEqual(0, MusicRules.NextTrack(MusicMode.Sequence, 0, 0, 7));
    }

    [Test]
    public void NextTrack_SingleTrackShuffle_ReturnsZero()
    {
        Assert.AreEqual(0, MusicRules.NextTrack(MusicMode.Shuffle, 0, 1, 5));
    }

    [Test]
    public void NextTrack_Loop_KeepsCurrent()
    {
        Assert.AreEqual(2, MusicRules.NextTrack(MusicMode.Loop, 2, 4, 9));
    }

    [Test]
    public void NextTrack_LoopCurrentOutOfRange_ReturnsZero()
    {
        Assert.AreEqual(0, MusicRules.NextTrack(MusicMode.Loop, 5, 3, 0));
    }

    [Test]
    public void NextTrack_Sequence_ReturnsNext()
    {
        Assert.AreEqual(2, MusicRules.NextTrack(MusicMode.Sequence, 1, 4, 0));
    }

    [Test]
    public void NextTrack_SequenceLastTrack_WrapsToFirst()
    {
        Assert.AreEqual(0, MusicRules.NextTrack(MusicMode.Sequence, 3, 4, 0));
    }

    [Test]
    public void NextTrack_Shuffle_NeverRepeatsCurrentAndStaysInRange()
    {
        for (int current = 0; current < 4; current++)
        {
            for (int random = 0; random < 20; random++)
            {
                int next = MusicRules.NextTrack(MusicMode.Shuffle, current, 4, random);
                Assert.AreNotEqual(current, next);
                Assert.That(next, Is.InRange(0, 3));
            }
        }
    }

    [Test]
    public void NextTrack_Shuffle_ReachesEveryOtherTrack()
    {
        var results = new HashSet<int>();
        for (int random = 0; random < 3; random++)
            results.Add(MusicRules.NextTrack(MusicMode.Shuffle, 1, 4, random));

        CollectionAssert.AreEquivalent(new[] { 0, 2, 3 }, results);
    }

    [Test]
    public void NextTrack_ShuffleNegativeRandom_StaysInRange()
    {
        int next = MusicRules.NextTrack(MusicMode.Shuffle, 0, 4, -7);
        Assert.That(next, Is.InRange(1, 3));
    }
}