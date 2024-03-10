using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class CardSkuModel
{
    public int Id { get; set; }

    public int Quantity { get; set; }

    public required string CardName { get; set; }

    public required string Edition { get; set; }

    public string? Language { get; set; }

    public string? DeckName { get; set; }

    public string? ContainerName { get; set; }

    public string? Comments { get; set; }

    public bool IsSideboard { get; set; }

    public bool IsFoil { get; set; }

    public CardCondition? Condition { get; set; }

    public bool IsLand { get; set; }
}
