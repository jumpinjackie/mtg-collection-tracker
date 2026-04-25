using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using ScryfallApi.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Services.Stubs;

public class StubCollectionTrackingService : ICollectionTrackingService
{
    public ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items, IScryfallApiClient? client, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<WishlistItemModel>> AddMultipleToWishlistAsync(IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> AddToDeckOrContainerAsync(int? containerId, int? deckId, AddToDeckOrContainerInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId, bool isCommander, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> DeleteCardSkuAsync(Guid skuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> DeleteNotesAsync(int id, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<WishlistBuyingListModel> GenerateBuyingListAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<CardSkuModel>> GetCardsAsync(CardQueryModel query, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<PaginatedCardSkuModel> GetCardsForContainerAsync(int containerId, FetchContainerPageModel options, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> GetCardSkuByIdAsync(Guid id, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CollectionSummaryModel> GetCollectionSummaryAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<ContainerSummaryModel>> GetContainersAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckModel> GetDeckAsync(int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<DeckSummaryModel>> GetDecksAsync(DeckFilterModel? filter, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<CardLanguageModel>> GetLanguagesAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<NotesModel>> GetNotesAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<VendorModel>> GetVendorsAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<WishlistItemModel>> GetWishlistItemsAsync(WishlistItemFilter filter, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<WishlistSpendSummaryModel> GetWishlistSpendAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> IsBasicLandAsync(string cardName, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(MoveWishlistItemsToCollectionInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<string> PrintDeckAsync(int deckId, DeckPrintOptions options, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(IEnumerable<string> cardNames, IScryfallApiClient client, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<CardSkuModel>> UpdateCardMetadataAsync(ICollection<Guid> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<WishlistItemModel> UpdateWishlistItemAsync(UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<string>> GetDeckFormatsAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> HasOtherDecksInFormatAsync(string format, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetLargeFrontFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetLargeBackFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetSmallFrontFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetSmallBackFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<WishlistItemModel>> UpdateWishlistMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<ApplyTagsResult> ApplyTagsAsync(IEnumerable<string> tags, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask AddMissingMetadataAsync(UpdateCardMetadataProgressCallback callback, IScryfallApiClient scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask RebuildAllMetadataAsync(UpdateCardMetadataProgressCallback callback, IScryfallApiClient scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask NormalizeCardNamesAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<ContainerSummaryModel> UpdateContainerAsync(int id, string name, string? description, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckSummaryModel> UpdateDeckAsync(int id, string name, string? format, int? containerId, bool isCommander, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckSummaryModel> SetDeckCommanderAsync(int deckId, Guid? commanderSkuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CommanderValidationResult> ValidateCommanderDeckAsync(int deckId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckSummaryModel> SetDeckBannerAsync(int deckId, Guid? cardSkuId, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<string> PrintContainerAsync(int containerId, ContainerPrintOptions options, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<List<LowestPriceCheckItem>> GetLowestPricesAsync(LowestPriceCheckOptions options, IScryfallApiClient client, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> IsScryfallIdMappingEmptyAsync(CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask ImportCardIdentifiersAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> ImportPriceDataAsync(UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(double? price, string? provider)> GetLatestPriceForSkuAsync(Guid skuId, string currency, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardPriceHistoryModel?> GetPriceHistoryForSkuAsync(Guid skuId, string currency, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }
}
