using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Messaging;

internal record CardsRemovedFromContainerMessage(int? ContainerId, int TotalSkus, List<int> SkuIds);

internal record CardsSentToContainerMessage(int ContainerId, int TotalSkus, string ContainerName, List<int> SkuIds);

internal record CardsRemovedFromDeckMessage(int? DeckId, int TotalSkus, List<int> SkuIds);

internal record CardsSentToDeckMessage(int DeckId, int TotalSkus, string DeckName, List<int> SkuIds);

internal record DeckTotalsChangedMessage(List<int> DeckIds);