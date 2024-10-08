﻿using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Messaging;

internal record CardsOrphanedMessage(List<int> SkuIds);

internal record CardsRemovedFromContainerMessage(int? ContainerId, List<int> SkuIds);

internal record CardsSentToContainerMessage(int ContainerId, string ContainerName, List<int> SkuIds);

internal record CardsRemovedFromDeckMessage(int? DeckId, List<int> SkuIds);

internal record CardsSentToDeckMessage(int DeckId, string DeckName, List<int> SkuIds);

internal record DeckTotalsChangedMessage(List<int> DeckIds);