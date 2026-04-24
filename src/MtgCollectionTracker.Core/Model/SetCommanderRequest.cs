namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for setting the commander card on a deck.</summary>
public record SetCommanderRequest(Guid? CommanderSkuId);
