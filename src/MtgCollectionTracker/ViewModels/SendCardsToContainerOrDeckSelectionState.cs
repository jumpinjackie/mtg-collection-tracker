namespace MtgCollectionTracker.ViewModels;

public sealed class SendCardsToContainerOrDeckSelectionState
{
    public int? LastContainerId { get; set; }

    public int? LastDeckId { get; set; }
}