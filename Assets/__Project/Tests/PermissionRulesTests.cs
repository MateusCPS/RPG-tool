using NUnit.Framework;

public class PermissionRulesTests
{
    const string Ana = "player-ana";
    const string Bruno = "player-bruno";

    [Test]
    public void CanMoveToken_MasterOnAnyToken_ReturnsTrue()
    {
        Assert.IsTrue(PermissionRules.CanMoveToken(TableRole.Master, Ana, Bruno));
    }
    
    [Test]
    public void CanManageTokens_Master_ReturnsTrue()
    {
        Assert.IsTrue(PermissionRules.CanManageTokens(TableRole.Master));
    }

    [Test]
    public void CanSeeHiddenTokens_Master_ReturnsTrue()
    {
        Assert.IsTrue(PermissionRules.CanSeeHiddenTokens(TableRole.Master));
    }

    [Test]
    public void CanMoveToken_PlayerOnOwnToken_ReturnsTrue()
    {
        Assert.IsTrue(PermissionRules.CanMoveToken(TableRole.Player, Ana, Ana));
    }

    [Test]
    public void CanChangeHp_PlayerOnOwnToken_ReturnsTrue()
    {
        Assert.IsTrue(PermissionRules.CanChangeHp(TableRole.Player, Ana, Ana));
    } 

    [Test]
    public void CanMoveToken_PlayerOnOtherPlayersToken_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanMoveToken(TableRole.Player, Ana, Bruno));
    }

    [Test]
    public void CanChangeHp_PlayerOnOtherPlayersToken_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanChangeHp(TableRole.Player, Ana, Bruno));
    } 

    [Test]
    public void CanMoveToken_PlayerOnUnownedToken_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanMoveToken(TableRole.Player, Ana, ""));
    }

    [Test]
    public void CanManageTokens_Player_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanManageTokens(TableRole.Player));
    }

    [Test]
    public void CanSeeHiddenTokens_Player_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanSeeHiddenTokens(TableRole.Player));
    }

    [Test]
    public void CanMoveToken_SpectatorOnOwnToken_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanMoveToken(TableRole.Spectator, Ana, Ana));
    }

    [Test]
    public void CanMoveToken_EmptyRequesterOnUnownedToken_ReturnsFalse()
    {
        Assert.IsFalse(PermissionRules.CanMoveToken(TableRole.Player, "", ""));
    }
}
