using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class WishlistItemModel
{
    public int Id { get; set; }

    public int Quantity { get; set; }

    public required string CardName { get; set; }

    public required string Edition { get; set; }

    public string? Language { get; set; }

    public string? CollectorNumber { get; set; }

    public bool IsFoil { get; set; }

    public CardCondition? Condition { get; set; }

    public bool IsLand { get; set; }

    public bool IsDoubleFaced { get; set; }

    public byte[]? ImageSmall { get; set; }

    public byte[]? BackImageSmall { get; set; }
}
