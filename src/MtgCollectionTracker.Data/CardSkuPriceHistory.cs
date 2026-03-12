using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// Stores a price data point for a <see cref="CardSku"/> at a specific date.
/// Prices have daily resolution: at most one entry per SKU per day.
/// </summary>
public class CardSkuPriceHistory
{
    public int Id { get; set; }

    /// <summary>
    /// The id of the <see cref="CardSku"/> this price entry belongs to
    /// </summary>
    public Guid CardSkuId { get; set; }

    /// <summary>
    /// The parent <see cref="CardSku"/>
    /// </summary>
    public virtual CardSku? CardSku { get; set; }

    /// <summary>
    /// The date this price was fetched (daily resolution)
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The USD price for the specific edition of the card SKU at this date
    /// </summary>
    public decimal? PriceUsd { get; set; }

    /// <summary>
    /// The USD price of the cheapest printing of this card across all editions at this date
    /// </summary>
    public decimal? CheapestPriceUsd { get; set; }

    /// <summary>
    /// The edition code with the cheapest price at this date
    /// </summary>
    [MaxLength(5)]
    public string? CheapestEdition { get; set; }
}
