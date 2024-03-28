using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Services.Stubs;

public class StubCollectionTrackingService : ICollectionTrackingService
{
    public ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items, IScryfallApiClient? client)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(int shortAmount, HashSet<string> fromDeckNames, HashSet<string> fromContainerNames)> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<ContainerInfoModel> CreateContainerAsync(string name, string? description)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckInfoModel> CreateDeckAsync(string name, string? format, int? containerId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> DeleteCardSkuAsync(int skuId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<CardSkuModel> GetCards(CardQueryModel query)
    {
        throw new System.NotImplementedException();
    }

    public CollectionSummaryModel GetCollectionSummary()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<ContainerSummaryModel> GetContainers()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<DeckSummaryModel> GetDecks(string? format)
    {
        throw new System.NotImplementedException();
    }

    public bool IsBasicLand(string cardName)
    {
        throw new System.NotImplementedException();
    }

    public string PrintDeck(int deckId, bool reportProxyUsage)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<int> UpdateCardSkuAsync(UpdateCardSkuInputModel model)
    {
        throw new System.NotImplementedException();
    }
}
