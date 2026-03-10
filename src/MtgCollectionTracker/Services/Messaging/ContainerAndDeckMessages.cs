using MtgCollectionTracker.Core.Model;
using System;
using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Messaging;

internal record CardsOrphanedMessage(List<Guid> SkuIds);

internal record CardsRemovedFromContainerMessage(int? ContainerId, List<Guid> SkuIds);

internal record CardsSentToContainerMessage(int ContainerId, string ContainerName, List<Guid> SkuIds);

internal record CardsRemovedFromDeckMessage(int? DeckId, List<Guid> SkuIds);

internal record CardsSentToDeckMessage(int DeckId, string DeckName, List<Guid> SkuIds);

internal record DeckSideboardChangedMessage(int DeckId, List<Guid> SkuIds, bool IsSideboard);

internal record DeckTotalsChangedMessage(List<int> DeckIds);

internal record DeckCreatedMessage(DeckSummaryModel Deck);

internal record DeckUpdatedMessage(DeckSummaryModel Deck);

internal record ContainerCreatedMessage(ContainerSummaryModel Container);

internal record ContainerUpdatedMessage(ContainerSummaryModel Container);

internal record WishlistItemsAddedToCollectionMessage(MoveWishlistItemsToCollectionResult Result);