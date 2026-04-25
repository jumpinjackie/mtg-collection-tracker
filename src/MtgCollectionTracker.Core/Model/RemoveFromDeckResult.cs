namespace MtgCollectionTracker.Core.Model;

/// <summary>Result returned when a card SKU is removed from a deck.</summary>
public record RemoveFromDeckResult(CardSkuModel Sku, bool WasMerged);
