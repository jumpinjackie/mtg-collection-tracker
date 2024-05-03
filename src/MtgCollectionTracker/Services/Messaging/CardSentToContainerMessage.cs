namespace MtgCollectionTracker.Services.Messaging;

internal record CardsSentToContainerMessage(int TotalSkus, string ContainerName);

internal record CardsSentToDeckMessage(int TotalSkus, string DeckName);
