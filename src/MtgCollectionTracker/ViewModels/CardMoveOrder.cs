namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// Specifies the order in which cards are moved to a destination zone
/// </summary>
public enum CardMoveOrder
{
    /// <summary>
    /// Cards are moved in the order they appear in the selection list
    /// </summary>
    AsSelected,

    /// <summary>
    /// Cards are moved in a randomly shuffled order
    /// </summary>
    Random,
}
