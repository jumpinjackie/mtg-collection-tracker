namespace MtgCollectionTracker.Core.Model;

/// <summary>
/// Represents the phases of a Magic: The Gathering turn
/// </summary>
public enum GamePhase
{
    Untap = 0,
    Upkeep = 1,
    Draw = 2,
    MainPhase1 = 3,
    BeginCombat = 4,
    DeclareAttackers = 5,
    DeclareBlockers = 6,
    CombatDamage = 7,
    EndCombat = 8,
    MainPhase2 = 9,
    End = 10,
    Cleanup = 11
}
