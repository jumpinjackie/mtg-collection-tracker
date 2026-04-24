namespace MtgCollectionTracker.Core.Model;

/// <summary>A grouped buying-list entry by vendor, as returned by the buying-list endpoint.</summary>
public record BuyingListVendorEntry(string Vendor, BuyingListItem[] Items);
