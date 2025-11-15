using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Services.Stubs;

public class StubCollectionTrackingService : ICollectionTrackingService
{
    public ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items, IScryfallApiClient? client)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<ICollection<WishlistItemModel>> AddMultipleToWishlistAsync(IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient)
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

    public ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(string cardName, int wantQty, bool noProxies, bool sparesOnly)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(ConsolidateCardSkusInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckSummaryModel> CreateDeckAsync(string name, string? format, int? containerId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> DeleteCardSkuAsync(int skuId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> DeleteNotesAsync(int id)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public WishlistBuyingListModel GenerateBuyingList()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<CardSkuModel> GetCards(CardQueryModel query)
    {
        throw new System.NotImplementedException();
    }

    public PaginatedCardSkuModel GetCardsForContainer(int containerId, FetchContainerPageModel options)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<CardSkuModel> GetCardSkuByIdAsync(int id, CancellationToken cancel)
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

    public ValueTask<DeckModel> GetDeckAsync(int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<DeckSummaryModel> GetDecks(DeckFilterModel filter)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<CardLanguageModel> GetLanguages()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<NotesModel> GetNotes()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<VendorModel> GetVendors()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<WishlistItemModel> GetWishlistItems(WishlistItemFilter filter)
    {
        throw new System.NotImplementedException();
    }

    public WishlistSpendSummaryModel GetWishlistSpend()
    {
        throw new System.NotImplementedException();
    }

    public bool IsBasicLand(string cardName)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(MoveWishlistItemsToCollectionInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public string PrintDeck(int deckId, DeckPrintOptions options)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(RemoveFromDeckInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(IEnumerable<string> cardNames, IScryfallApiClient client)
    {
        throw new System.NotImplementedException();
    }

    public Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<WishlistItemModel> UpdateWishlistItemAsync(UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<string> GetDeckFormats()
    {
        throw new System.NotImplementedException();
    }

    public bool HasOtherDecksInFormat(string format)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<IEnumerable<WishlistItemModel>> UpdateWishlistMetadataAsync(ICollection<int> ids, IScryfallApiClient scryfallApiClient, UpdateCardMetadataProgressCallback? callback, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<string> GetTags()
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

    public ValueTask<ContainerSummaryModel> UpdateContainerAsync(int id, string name, string? description)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<DeckSummaryModel> UpdateDeckAsync(int id, string name, string? format, int? containerId)
    {
        throw new System.NotImplementedException();
    }

    public string PrintContainer(int containerId, ContainerPrintOptions options)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<List<LowestPriceCheckItem>> GetLowestPricesAsync(LowestPriceCheckOptions options, IScryfallApiClient client, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }
}
