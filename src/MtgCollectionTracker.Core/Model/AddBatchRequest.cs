namespace MtgCollectionTracker.Core.Model;

/// <summary>Request body for adding a batch of cards to a container or deck.</summary>
public record AddBatchRequest(int? ContainerId, int? DeckId, IEnumerable<AddToDeckOrContainerInputModel> Items);
