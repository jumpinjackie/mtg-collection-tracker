namespace MtgCollectionTracker.Services.Messaging;

internal record CardsSentToContainerMessage(int TotalSkus, string ContainerName);
