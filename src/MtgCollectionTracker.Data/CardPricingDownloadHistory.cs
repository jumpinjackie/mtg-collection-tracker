namespace MtgCollectionTracker.Data;

/// <summary>
/// Tracks downloaded MTG JSON price data sets by SHA256 hash to avoid re-importing the same data.
/// </summary>
public class CardPricingDownloadHistory
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public required string Sha256 { get; set; }
}
