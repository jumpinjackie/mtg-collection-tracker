using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Tests;

internal static class CollectionTrackingServiceTestExtensions
{
    public static bool IsBasicLand(this ICollectionTrackingService service, string cardName)
        => service.IsBasicLandAsync(cardName, CancellationToken.None).GetAwaiter().GetResult();

    public static CollectionSummaryModel GetCollectionSummary(this ICollectionTrackingService service)
        => service.GetCollectionSummaryAsync(CancellationToken.None).GetAwaiter().GetResult();

    public static IReadOnlyList<ContainerSummaryModel> GetContainers(this ICollectionTrackingService service)
        => service.GetContainersAsync(CancellationToken.None).GetAwaiter().GetResult();

    public static IReadOnlyList<DeckSummaryModel> GetDecks(this ICollectionTrackingService service, DeckFilterModel? filter)
        => service.GetDecksAsync(filter, CancellationToken.None).GetAwaiter().GetResult();

    public static IReadOnlyList<CardSkuModel> GetCards(this ICollectionTrackingService service, CardQueryModel query)
        => service.GetCardsAsync(query, CancellationToken.None).GetAwaiter().GetResult();

    public static ValueTask<ContainerSummaryModel> CreateContainerAsync(this ICollectionTrackingService service, string name, string? description)
        => service.CreateContainerAsync(name, description, CancellationToken.None);

    public static ValueTask<DeckSummaryModel> CreateDeckAsync(this ICollectionTrackingService service, string name, string? format, int? containerId)
        => service.CreateDeckAsync(name, format, containerId, false, CancellationToken.None);

    public static ValueTask<DeckSummaryModel> CreateDeckAsync(this ICollectionTrackingService service, string name, string? format, int? containerId, bool isCommander)
        => service.CreateDeckAsync(name, format, containerId, isCommander, CancellationToken.None);

    public static ValueTask<DismantleDeckResult> DismantleDeckAsync(this ICollectionTrackingService service, DismantleDeckInputModel model)
        => service.DismantleDeckAsync(model, CancellationToken.None);

    public static ValueTask<CardSkuModel> AddToDeckOrContainerAsync(this ICollectionTrackingService service, int? containerId, int? deckId, AddToDeckOrContainerInputModel model)
        => service.AddToDeckOrContainerAsync(containerId, deckId, model, CancellationToken.None);

    public static ValueTask<DeckSummaryModel> UpdateDeckAsync(this ICollectionTrackingService service, int id, string name, string? format, int? containerId, bool isCommander)
        => service.UpdateDeckAsync(id, name, format, containerId, isCommander, CancellationToken.None);

    public static ValueTask<DeckSummaryModel> SetDeckCommanderAsync(this ICollectionTrackingService service, int deckId, Guid? commanderSkuId)
        => service.SetDeckCommanderAsync(deckId, commanderSkuId, CancellationToken.None);
}
