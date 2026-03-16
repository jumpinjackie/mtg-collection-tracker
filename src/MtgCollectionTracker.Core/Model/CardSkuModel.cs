using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

/// <summary>The cheapest retail price entry for a card SKU in the configured currency.</summary>
public class CardSkuPriceModel
{
    public double Price { get; set; }

    public required string Provider { get; set; }
}

public class CardSkuModel
{
    public Guid Id { get; set; }

    public string? ScryfallId { get; set; }

    public int Quantity { get; set; }

    public required string CardName { get; set; }

    public required string Edition { get; set; }

    public string? Language { get; set; }

    public string? CollectorNumber { get; set; }

    public int? DeckId { get; set; }

    public string? DeckName { get; set; }

    public int? ContainerId { get; set; }

    public string? ContainerName { get; set; }

    public string? Comments { get; set; }

    public bool IsSideboard { get; set; }

    public bool IsFoil { get; set; }

    public CardCondition? Condition { get; set; }

    public bool IsLand { get; set; }

    public bool IsDoubleFaced { get; set; }

    public string[] Tags { get; set; }

    public string? CastingCost { get; set; }

    public string? OracleText { get; set; }

    public string? CardType { get; set; }

    public string? Power { get; set; }

    public string? Toughness { get; set; }

    public string? BackPower { get; set; }

    public string? BackToughness { get; set; }

    public string? Loyalty { get; set; }

    public string? BackLoyalty { get; set; }

    public string[]? Colors { get; set; }

    public string[]? ColorIdentity { get; set; }

    /// <summary>The cheapest retail price for this card in the configured currency, from MTG JSON price data. Null if no price data is available.</summary>
    public CardSkuPriceModel? LatestPrice { get; set; }

    public string GetDescription(bool withId = true) => withId
        ? $"({Quantity}x {CardName}, {Edition}, id: {Id})"
        : $"({Quantity}x {CardName}, {Edition})";
}
