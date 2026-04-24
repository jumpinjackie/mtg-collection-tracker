namespace MtgCollectionTracker.Core.Model;

/// <summary>The latest price for a card SKU.</summary>
public record SkuPriceResult(double? Price, string? Provider);
