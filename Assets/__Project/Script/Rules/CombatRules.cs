public static class CombatRules
{
    public const float Tolerance = 0.05f;

    public static bool CanMove(bool combatActive, TableRole role, float movedThisRound, float distance, float speed) => 
        !combatActive || role == TableRole.Master || movedThisRound + distance <= speed + Tolerance;
}
