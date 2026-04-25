namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for setting the banner card on a deck.</summary>
public record SetBannerRequest(Guid? CardSkuId);
