using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Messaging;

internal record CardsSentToContainerMessage(int ContainerId, int TotalSkus, string ContainerName, List<int> SkuIds);

internal record CardsSentToDeckMessage(int DeckId, int TotalSkus, string DeckName, List<int> SkuIds);
