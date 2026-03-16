using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// A price entry from the MTG JSON cardPrices.csv feed.
/// </summary>
public class CardPricingEntry
{
    public int Id { get; set; }

    [MaxLength(36)]
    public required string Uuid { get; set; }

    [MaxLength(20)]
    public required string CardFinish { get; set; }

    [MaxLength(3)]
    public required string Currency { get; set; }

    public DateOnly Date { get; set; }

    [MaxLength(20)]
    public string? GameAvailability { get; set; }

    public double Price { get; set; }

    [MaxLength(50)]
    public required string PriceProvider { get; set; }

    [MaxLength(20)]
    public required string ProviderListing { get; set; }

    /// <summary>
    /// Mapping record for the MTG JSON UUID associated with this price entry.
    /// </summary>
    public virtual ScryfallIdMapping? ScryfallIdMapping { get; set; }
}
