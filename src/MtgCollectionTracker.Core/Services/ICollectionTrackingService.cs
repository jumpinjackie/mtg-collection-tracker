using MtgCollectionTracker.Core.Model;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Core.Services
{
    public class UpdateCardMetadataProgressCallback
    {
        public int ReportFrequency { get; set; } = 10;

        public Action<int, int>? OnProgress { get; set; }
    }

    public record ApplyTagsResult(int Added, int Deleted, int Detached, List<string> CurrentTags);

    public record CheckQuantityResult(int ShortAmount, HashSet<string> FromDeckNames, HashSet<string> FromContainerNames, string? SuggestedName);

    public interface ICollectionTrackingService
    {
        IEnumerable<CardLanguageModel> GetLanguages();
        ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items, IScryfallApiClient? scryfallClient);
        ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model);
        ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model);
        ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly);
        ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model);
        ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description);
        ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId);
        ValueTask<CardSkuModel> DeleteCardSkuAsync(int skuId);
        ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model);
        ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel);
        ValueTask<IEnumerable<WishlistItemModel>> UpdateWishlistMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel);
        IEnumerable<CardSkuModel> GetCards(CardQueryModel query);
        ValueTask<CardSkuModel> GetCardSkuByIdAsync(int id, CancellationToken cancel);
        PaginatedCardSkuModel GetCardsForContainer(int containerId, FetchContainerPageModel options);
        IEnumerable<ContainerSummaryModel> GetContainers();
        IEnumerable<DeckSummaryModel> GetDecks(DeckFilterModel? filter);
        bool IsBasicLand(string cardName);
        string PrintDeck(int deckId, bool reportProxyUsage);
        ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model);
        Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model);
        ValueTask<int> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        CollectionSummaryModel GetCollectionSummary();
        ValueTask<ICollection<WishlistItemModel>> AddMultipleToWishlistAsync(IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient);
        IEnumerable<WishlistItemModel> GetWishlistItems();
        ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model);
        IEnumerable<VendorModel> GetVendors();
        ValueTask<WishlistItemModel> UpdateWishlistItemAsync(UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        WishlistSpendSummaryModel GetWishlistSpend();
        ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id);
        ValueTask<DeckModel> GetDeckAsync(int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(MoveWishlistItemsToCollectionInputModel model);
        ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model);
        IEnumerable<NotesModel> GetNotes();
        ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes);
        ValueTask<bool> DeleteNotesAsync(int id);
        ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(IEnumerable<string> cardNames, IScryfallApiClient client);
        WishlistBuyingListModel GenerateBuyingList();
        IEnumerable<string> GetDeckFormats();
        bool HasOtherDecksInFormat(string format);
        ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId);
        ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId);
        ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId);
        ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId);
        IEnumerable<string> GetTags();
        ValueTask<ApplyTagsResult> ApplyTagsAsync(IEnumerable<string> tags, CancellationToken cancel);
    }
}