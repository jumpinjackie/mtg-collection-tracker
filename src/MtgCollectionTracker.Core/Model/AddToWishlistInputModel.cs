using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class AddToWishlistInputModel
{
    public int Quantity { get; set; }

    public required string CardName { get; set; }

    public required string Edition { get; set; }

    public string? CollectorNumber { get; set; }

    public string? Language { get; set; }

    public int? DeckId { get; set; }

    public bool IsSideboard { get; set; }

    public bool IsFoil { get; set; }

    public CardCondition? Condition { get; set; }

    public string? Comments { get; set; }

    public bool IsLand { get; set; }
}
