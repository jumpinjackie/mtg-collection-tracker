using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.Services.Messaging;

internal class DeckCreatedMessage
{
    public required DeckSummaryModel Deck { get; set; }
}
