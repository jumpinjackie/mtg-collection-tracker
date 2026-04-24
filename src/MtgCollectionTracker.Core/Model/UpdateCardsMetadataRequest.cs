namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for triggering a card-metadata update operation.</summary>
public record UpdateCardsMetadataRequest(ICollection<Guid> Ids);
