using MtgCollectionTracker.Core.Model;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Core.Services
{
    public class UpdateCardMetadataProgressCallback
    {
        public int ReportFrequency { get; set; } = 10;

        public Action<int, int>? OnProgress { get; set; }

        /// <summary>Fires during the download phase with a human-readable status string (e.g. "Downloading … 12 MB / 45 MB").</summary>
        public Action<string>? OnDownloadStatus { get; set; }
    }

    public record WishlistItemFilter(IEnumerable<string>? Tags);

    public record DeckPrintOptions(bool ReportProxyUsage);

    public record ContainerPrintOptions(bool ReportProxyUsage);

    public record struct ApplyTagsResult(int Added, int Deleted, int Detached, List<string> CurrentTags);

    public record struct CheckQuantityResult(int ShortAmount, HashSet<string> FromDeckNames, HashSet<string> FromContainerNames, string? SuggestedName, int WishlistAmount);

    public record struct SkuUpdateInfo(Guid Id, int OldQuantity, int NewQuantity, int? OldDeckId, int? NewDeckId, int? OldContainerId, int? NewContainerId);

    public record struct UpdateCardSkuResult(int RecordsAffected, List<SkuUpdateInfo> Skus)
    {
        public IEnumerable<int> DeckChangedTotals() => Skus.Where(s => s.OldQuantity != s.NewQuantity && s.OldDeckId.HasValue && s.OldDeckId == s.NewDeckId).Select(s => s.OldDeckId.Value);

        public IEnumerable<SkuUpdateInfo> ChangedDecks() => Skus.Where(s => s.OldDeckId != s.NewDeckId);

        public IEnumerable<SkuUpdateInfo> ChangedContainer() => Skus.Where(s => s.OldContainerId != s.NewContainerId);

        public IEnumerable<SkuUpdateInfo> Orphaned() => Skus.Where(s => s.OldContainerId.HasValue && !s.NewContainerId.HasValue);
    }

    public record struct PriceCheckItem(string CardName, int Quantity);

    public record struct LowestPriceCheckItem(string CardName, string? Edition, int Quantity, decimal? ItemTotal)
    {
        public decimal? QuantityTotal
        {
            get
            {
                if (ItemTotal.HasValue)
                {
                    return ItemTotal.Value * Quantity;
                }
                return null;
            }
        }
    }

    public record LowestPriceCheckOptions(IEnumerable<PriceCheckItem> Items, bool SkipBasicLands, bool SkipSnowBasicLands);

    public record LowestPriceCheckForDeckOptions(int DeckId, bool SkipBasicLands, bool SkipSnowBasicLands);

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
        ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId, bool isCommander = false);
        ValueTask<DeckSummaryModel> UpdateDeckAsync(int id, string name, string? format, int? containerId, bool isCommander = false);
        ValueTask<DeckSummaryModel> SetDeckBannerAsync(int deckId, Guid? cardSkuId);
        ValueTask<CardSkuModel> DeleteCardSkuAsync(Guid skuId);
        ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model);
        ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(ICollection<Guid> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel);
        ValueTask<IEnumerable<WishlistItemModel>> UpdateWishlistMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel);
        IEnumerable<CardSkuModel> GetCards(CardQueryModel query);
        ValueTask<CardSkuModel> GetCardSkuByIdAsync(Guid id, CancellationToken cancel);
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
        ValueTask<List<LowestPriceCheckItem>> GetLowestPricesAsync(LowestPriceCheckOptions options, CancellationToken cancel);

        /// <summary>Checks if the ScryfallIdMapping table is empty (i.e., card identifiers have not been imported).</summary>
        ValueTask<bool> IsScryfallIdMappingEmptyAsync(CancellationToken cancel);

        /// <summary>Downloads and imports card identifiers from MTG JSON. Clears existing data first on re-import.</summary>
        ValueTask ImportCardIdentifiersAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel);

        /// <summary>Downloads and imports the latest price data from MTG JSON. Skips if the latest sha256 matches what's already been imported.</summary>
        /// <returns>True if new data was imported, false if already up-to-date.</returns>
        ValueTask<bool> ImportPriceDataAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel);

        /// <summary>Checks whether local pricing data exists for lowest price checks.</summary>
        ValueTask<bool> HasLocalPriceDataAsync(CancellationToken cancel);

        /// <summary>Gets the latest price for a card SKU by its ID.</summary>
        ValueTask<(double? price, string? provider)> GetLatestPriceForSkuAsync(Guid skuId, string currency, CancellationToken cancel);

        /// <summary>Gets the price history for a card SKU for up to the 10 most recent dates.</summary>
        ValueTask<CardPriceHistoryModel?> GetPriceHistoryForSkuAsync(Guid skuId, string currency, CancellationToken cancel);

        /// <summary>Validates a commander deck against commander rules. Returns a validation result with any errors.</summary>
        ValueTask<CommanderValidationResult> ValidateCommanderDeckAsync(int deckId, CancellationToken cancel);

        /// <summary>Sets the commander card for a commander deck.</summary>
        ValueTask<DeckSummaryModel> SetDeckCommanderAsync(int deckId, Guid? commanderSkuId);
    }

    public static class CollectionTrackingServiceExtensions
    {
        public static async ValueTask<List<LowestPriceCheckItem>> GetLowestPricesForDeckAsync(this ICollectionTrackingService service, LowestPriceCheckForDeckOptions options, IScryfallApiClient? client, CancellationToken cancel)
        {
            var deck = await service.GetDeckAsync(options.DeckId, client, cancel);
            
            var items = new List<PriceCheckItem>();

            items.AddRange(deck.MainDeck.GroupBy(c => c.CardName).Select(grp => new PriceCheckItem(grp.Key, grp.Count())));
            items.AddRange(deck.Sideboard.GroupBy(c => c.CardName).Select(grp => new PriceCheckItem(grp.Key, grp.Count())));

            var checkOpts = new LowestPriceCheckOptions(items, options.SkipBasicLands, options.SkipSnowBasicLands);

            var res = await service.GetLowestPricesAsync(checkOpts, cancel);
            return res;
        }
    }
}