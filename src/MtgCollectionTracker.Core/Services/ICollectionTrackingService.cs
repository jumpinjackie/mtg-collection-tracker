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
        ValueTask<IReadOnlyList<CardLanguageModel>> GetLanguagesAsync(CancellationToken cancel);
        ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items, IScryfallApiClient? scryfallClient, CancellationToken cancel);
        ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model, CancellationToken cancel);
        ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model, CancellationToken cancel);
        ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly, CancellationToken cancel);
        ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model, CancellationToken cancel);
        ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description, CancellationToken cancel);
        ValueTask<ContainerSummaryModel> UpdateContainerAsync(int id, string name, string? description, CancellationToken cancel);
        ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId, bool isCommander, CancellationToken cancel);
        ValueTask<DeckSummaryModel> UpdateDeckAsync(int id, string name, string? format, int? containerId, bool isCommander, CancellationToken cancel);
        ValueTask<DeckSummaryModel> SetDeckBannerAsync(int deckId, Guid? cardSkuId, CancellationToken cancel);
        ValueTask<CardSkuModel> DeleteCardSkuAsync(Guid skuId, CancellationToken cancel);
        ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model, CancellationToken cancel);
        ValueTask<IReadOnlyList<CardSkuModel>> UpdateCardMetadataAsync(ICollection<Guid> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel);
        ValueTask<IReadOnlyList<WishlistItemModel>> UpdateWishlistMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel);
        ValueTask<IReadOnlyList<CardSkuModel>> GetCardsAsync(CardQueryModel query, CancellationToken cancel);
        ValueTask<CardSkuModel> GetCardSkuByIdAsync(Guid id, CancellationToken cancel);
        ValueTask<PaginatedCardSkuModel> GetCardsForContainerAsync(int containerId, FetchContainerPageModel options, CancellationToken cancel);
        ValueTask<IReadOnlyList<ContainerSummaryModel>> GetContainersAsync(CancellationToken cancel);
        ValueTask<IReadOnlyList<DeckSummaryModel>> GetDecksAsync(DeckFilterModel? filter, CancellationToken cancel);
        ValueTask<bool> IsBasicLandAsync(string cardName, CancellationToken cancel);
        ValueTask<string> PrintDeckAsync(int deckId, DeckPrintOptions options, CancellationToken cancel);
        ValueTask<string> PrintContainerAsync(int containerId, ContainerPrintOptions options, CancellationToken cancel);
        ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model, CancellationToken cancel);
        ValueTask<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model, CancellationToken cancel);
        ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        ValueTask<CollectionSummaryModel> GetCollectionSummaryAsync(CancellationToken cancel);
        ValueTask<IReadOnlyList<WishlistItemModel>> AddMultipleToWishlistAsync(IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient, CancellationToken cancel);
        ValueTask<IReadOnlyList<WishlistItemModel>> GetWishlistItemsAsync(WishlistItemFilter filter, CancellationToken cancel);
        ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model, CancellationToken cancel);
        ValueTask<IReadOnlyList<VendorModel>> GetVendorsAsync(CancellationToken cancel);
        ValueTask<WishlistItemModel> UpdateWishlistItemAsync(UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        ValueTask<WishlistSpendSummaryModel> GetWishlistSpendAsync(CancellationToken cancel);
        ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id, CancellationToken cancel);
        ValueTask<DeckModel> GetDeckAsync(int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel);
        ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(MoveWishlistItemsToCollectionInputModel model, CancellationToken cancel);
        ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model, CancellationToken cancel);
        ValueTask<IReadOnlyList<NotesModel>> GetNotesAsync(CancellationToken cancel);
        ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes, CancellationToken cancel);
        ValueTask<bool> DeleteNotesAsync(int id, CancellationToken cancel);
        ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(IEnumerable<string> cardNames, IScryfallApiClient client, CancellationToken cancel);
        ValueTask<WishlistBuyingListModel> GenerateBuyingListAsync(CancellationToken cancel);
        ValueTask<IReadOnlyList<string>> GetDeckFormatsAsync(CancellationToken cancel);
        ValueTask<bool> HasOtherDecksInFormatAsync(string format, CancellationToken cancel);

        /// <summary>Fetches the large front-face image for the card SKU with the given id.</summary>
        ValueTask<Stream?> GetLargeFrontFaceImageAsync(Guid cardSkuId, CancellationToken cancel);

        /// <summary>Fetches the large back-face image for the card SKU with the given id.</summary>
        ValueTask<Stream?> GetLargeBackFaceImageAsync(Guid cardSkuId, CancellationToken cancel);

        /// <summary>Fetches the small front-face image for the card SKU with the given id.</summary>
        ValueTask<Stream?> GetSmallFrontFaceImageAsync(Guid cardSkuId, CancellationToken cancel);

        /// <summary>Fetches the small back-face image for the card SKU with the given id.</summary>
        ValueTask<Stream?> GetSmallBackFaceImageAsync(Guid cardSkuId, CancellationToken cancel);

        /// <summary>Fetches the large front-face image for the wishlist item with the given id.</summary>
        ValueTask<Stream?> GetLargeFrontFaceImageAsync(int wishlistItemId, CancellationToken cancel);

        /// <summary>Fetches the large back-face image for the wishlist item with the given id.</summary>
        ValueTask<Stream?> GetLargeBackFaceImageAsync(int wishlistItemId, CancellationToken cancel);

        /// <summary>Fetches the small front-face image for the wishlist item with the given id.</summary>
        ValueTask<Stream?> GetSmallFrontFaceImageAsync(int wishlistItemId, CancellationToken cancel);

        /// <summary>Fetches the small back-face image for the wishlist item with the given id.</summary>
        ValueTask<Stream?> GetSmallBackFaceImageAsync(int wishlistItemId, CancellationToken cancel);
        ValueTask<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancel);
        ValueTask<ApplyTagsResult> ApplyTagsAsync(IEnumerable<string> tags, CancellationToken cancel);
        ValueTask AddMissingMetadataAsync(UpdateCardMetadataProgressCallback callback, IScryfallApiClient scryfallApiClient, CancellationToken cancel);
        ValueTask RebuildAllMetadataAsync(UpdateCardMetadataProgressCallback callback, IScryfallApiClient scryfallApiClient, CancellationToken cancel);
        ValueTask NormalizeCardNamesAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel);
        ValueTask<List<LowestPriceCheckItem>> GetLowestPricesAsync(LowestPriceCheckOptions options, IScryfallApiClient client, CancellationToken cancel);

        /// <summary>Checks if the ScryfallIdMapping table is empty (i.e., card identifiers have not been imported).</summary>
        ValueTask<bool> IsScryfallIdMappingEmptyAsync(CancellationToken cancel);

        /// <summary>Downloads and imports card identifiers from MTG JSON. Clears existing data first on re-import.</summary>
        ValueTask ImportCardIdentifiersAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel);

        /// <summary>Downloads and imports the latest price data from MTG JSON. Skips if the latest sha256 matches what's already been imported.</summary>
        /// <returns>True if new data was imported, false if already up-to-date.</returns>
        ValueTask<bool> ImportPriceDataAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel);

        /// <summary>Gets the latest price for a card SKU by its ID.</summary>
        ValueTask<(double? price, string? provider)> GetLatestPriceForSkuAsync(Guid skuId, string currency, CancellationToken cancel);

        /// <summary>Gets the price history for a card SKU for up to the 10 most recent dates.</summary>
        ValueTask<CardPriceHistoryModel?> GetPriceHistoryForSkuAsync(Guid skuId, string currency, CancellationToken cancel);

        /// <summary>Validates a commander deck against commander rules. Returns a validation result with any errors.</summary>
        ValueTask<CommanderValidationResult> ValidateCommanderDeckAsync(int deckId, CancellationToken cancel);

        /// <summary>Sets the commander card for a commander deck.</summary>
        ValueTask<DeckSummaryModel> SetDeckCommanderAsync(int deckId, Guid? commanderSkuId, CancellationToken cancel);
    }

    public static class CollectionTrackingServiceExtensions
    {
        public static async ValueTask<List<LowestPriceCheckItem>> GetLowestPricesForDeckAsync(this ICollectionTrackingService service, LowestPriceCheckForDeckOptions options, IScryfallApiClient client, CancellationToken cancel)
        {
            var deck = await service.GetDeckAsync(options.DeckId, client, cancel);

            var items = new List<PriceCheckItem>();

            items.AddRange(deck.MainDeck.GroupBy(c => c.CardName).Select(grp => new PriceCheckItem(grp.Key, grp.Count())));
            items.AddRange(deck.Sideboard.GroupBy(c => c.CardName).Select(grp => new PriceCheckItem(grp.Key, grp.Count())));

            var checkOpts = new LowestPriceCheckOptions(items, options.SkipBasicLands, options.SkipSnowBasicLands);

            var res = await service.GetLowestPricesAsync(checkOpts, client, cancel);
            return res;
        }
    }
}
