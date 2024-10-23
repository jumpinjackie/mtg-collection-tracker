using MtgCollectionTracker.Core.Model;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Core.Services
{
    public class UpdateCardMetadataProgressCallback
    {
        public int ReportFrequency { get; set; } = 10;

        public Action<int, int>? OnProgress { get; set; }
    }

    public record WishlistItemFilter(IEnumerable<string>? Tags);

    public record DeckPrintOptions(bool ReportProxyUsage, bool ReportLoanUsage = false);

    public record ContainerPrintOptions(bool ReportProxyUsage);

    public record struct ApplyTagsResult(int Added, int Deleted, int Detached, List<string> CurrentTags);

    public record struct CheckQuantityResult(int ShortAmount, HashSet<string> FromDeckNames, HashSet<string> FromContainerNames, string? SuggestedName, int WishlistAmount);

    public record struct SkuUpdateInfo(int Id, int OldQuantity, int NewQuantity, int? OldDeckId, int? NewDeckId, int? OldContainerId, int? NewContainerId);

    public record struct UpdateCardSkuResult(int RecordsAffected, List<SkuUpdateInfo> Skus)
    {
        public IEnumerable<int> DeckChangedTotals() => Skus.Where(s => s.OldQuantity != s.NewQuantity && s.OldDeckId.HasValue && s.OldDeckId == s.NewDeckId).Select(s => s.OldDeckId.Value);

        public IEnumerable<SkuUpdateInfo> ChangedDecks() => Skus.Where(s => s.OldDeckId != s.NewDeckId);

        public IEnumerable<SkuUpdateInfo> ChangedContainer() => Skus.Where(s => s.OldContainerId != s.NewContainerId);

        public IEnumerable<SkuUpdateInfo> Orphaned() => Skus.Where(s => s.OldContainerId.HasValue && !s.NewContainerId.HasValue);
    }

    public interface ICollectionTrackingService
    {
        IEnumerable<CardLanguageModel> GetLanguages();
        ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items, IScryfallApiClient? scryfallClient);
        ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model);
        ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model);
        ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly);
        ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model);
        ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description);
        ValueTask<ContainerSummaryModel> UpdateContainerAsync(int id, string name, string? description);
        ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId);
        ValueTask<DeckSummaryModel> UpdateDeckAsync(int id, string name, string? format, int? containerId);
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
        string PrintDeck(int deckId, DeckPrintOptions options);
        string PrintContainer(int containerId, ContainerPrintOptions options);
        ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model);
        Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model);
        ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        CollectionSummaryModel GetCollectionSummary();
        ValueTask<ICollection<WishlistItemModel>> AddMultipleToWishlistAsync(IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient);
        IEnumerable<WishlistItemModel> GetWishlistItems(WishlistItemFilter filter);
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
        ValueTask AddMissingMetadataAsync(UpdateCardMetadataProgressCallback callback, IScryfallApiClient scryfallApiClient, CancellationToken cancel);
        ValueTask RebuildAllMetadataAsync(UpdateCardMetadataProgressCallback callback, IScryfallApiClient scryfallApiClient, CancellationToken cancel);
        ValueTask NormalizeCardNamesAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel);
        ValueTask<LoanModel> CreateLoanAsync(string name, int toDeckId, CancellationToken cancel);
        ValueTask<IEnumerable<LoanModel>> GetLoansAsync(CancellationToken cancel);
        ValueTask<LoanModel> UpdateLoanAsync(UpdateLoanModel model, CancellationToken cancel);
        ValueTask<LoanModel> DeleteLoanAsync(int id, CancellationToken cancel);
    }
}