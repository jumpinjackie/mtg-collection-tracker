using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.Core.Services
{
    public interface ICollectionTrackingService
    {
        ValueTask<(int total, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items);
        ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model);
        ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model);
        ValueTask<(int shortAmount, HashSet<string> fromDeckNames, HashSet<string> fromContainerNames)> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly);
        ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model);
        ValueTask<ContainerInfoModel> CreateContainerAsync(string name, string? description);
        ValueTask<DeckInfoModel> CreateDeckAsync(string name, string? format, int? containerId);
        ValueTask<CardSkuModel> DeleteCardSkuAsync(int skuId);
        ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model);
        IEnumerable<CardSkuModel> GetCards(CardQueryModel query);
        IEnumerable<ContainerSummaryModel> GetContainers();
        IEnumerable<DeckSummaryModel> GetDecks(string? format);
        bool IsBasicLand(string cardName);
        string PrintDeck(int deckId, bool reportProxyUsage);
        ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model);
        Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model);
        ValueTask<int> UpdateCardSkuAsync(UpdateCardSkuInputModel model);
    }
}