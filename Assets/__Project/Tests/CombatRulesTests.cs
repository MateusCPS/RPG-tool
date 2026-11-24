using NUnit.Framework;

public class CombatRulesTests
{
    const float Speed = 9f;

    [Test]
    public void CanMove_OutOfCombat_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanMove(false, TableRole.Player, 0f, 30f, Speed));
    }

    [Test]
    public void CanMove_PlayerWithinSpeed_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanMove(true, TableRole.Player, 0f, 6f, Speed));
    }

    [Test]
    public void CanMove_PlayerExactlySpeed_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanMove(true, TableRole.Player, 4.5f, 4.5f, Speed));
    }

    [Test]
    public void CanMove_PlayerOverSpeed_ReturnsFalse()
    {
        Assert.IsFalse(CombatRules.CanMove(true, TableRole.Player, 0f, 10.5f, Speed));
    }

    [Test]
    public void CanMove_PlayerOverAfterPreviousMoves_ReturnsFalse()
    {
        Assert.IsFalse(CombatRules.CanMove(true, TableRole.Player, 7.5f, 3f, Speed));
    }

    [Test]
    public void CanMove_MasterOverSpeed_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanMove(true, TableRole.Master, 0f, 30f, Speed));
    }

    [Test]
    public void CanMove_FloatErrorWithinTolerance_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanMove(true, TableRole.Player, 0f, 9.03f, Speed));
    }

    [Test]
    public void CanMove_JustOverTolerance_ReturnsFalse()
    {
        Assert.IsFalse(CombatRules.CanMove(true, TableRole.Player, 0f, 9.1f, Speed));
    }

    [Test]
    public void CanUndoMove_MasterNotMover_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanUndoMove(TableRole.Master, false));
    }

    [Test]
    public void CanUndoMove_PlayerMover_ReturnsTrue()
    {
        Assert.IsTrue(CombatRules.CanUndoMove(TableRole.Player, true));
    }

    [Test]
    public void CanUndoMove_PlayerNotMover_ReturnsFalse()
    {
        Assert.IsFalse(CombatRules.CanUndoMove(TableRole.Player, false));
    }

    [Test]
    public void RemainingMovement_PartlyMoved_ReturnsDifference()
    {
        Assert.AreEqual(4.5f, CombatRules.RemainingMovement(4.5f, Speed), 0.001f);
    }

    [Test]
    public void RemainingMovement_MovedMoreThanSpeed_ReturnsZero()
    {
        Assert.AreEqual(0f, CombatRules.RemainingMovement(10.5f, Speed), 0.001f);
    }
}