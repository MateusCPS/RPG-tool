
public static class PermissionRules
{
    public static bool CanMoveToken(TableRole role, string requesterId, string ownerId) =>
        role == TableRole.Master || (role == TableRole.Player && Owns(requesterId, ownerId));
    public static bool CanChangeHp(TableRole role, string requesterId, string ownerId) =>
        role == TableRole.Master || (role == TableRole.Player && Owns(requesterId, ownerId));
    
    public static bool CanManageTokens(TableRole role) => role == TableRole.Master;
    public static bool CanChangeTable(TableRole role) => role == TableRole.Master;
    public static bool CanSeeHiddenTokens(TableRole role) => role == TableRole.Master;

    static bool Owns(string requesterId, string ownerId) =>
        !string.IsNullOrEmpty(ownerId) && requesterId == ownerId;
}
