namespace MtgCollectionTracker.Services.Messaging;

internal class DeckDismantledMessage
{
    public int Id { get; set; }

    public required string Format { get; set; }
}
