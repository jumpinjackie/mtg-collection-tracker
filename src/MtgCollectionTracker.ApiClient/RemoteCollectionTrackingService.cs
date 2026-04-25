using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gen = MtgCollectionTracker.ApiClient.Generated;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using ScryfallApi.Client;

namespace MtgCollectionTracker.ApiClient;

// ── Internal helpers ──────────────────────────────────────────────────────────

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812", Justification = "Used for SSE deserialization")]
internal sealed class ProgressEventDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("current")]
    public int Current { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

internal sealed record OperationStartedDto(
    [property: JsonPropertyName("operationId")] string OperationId);

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Implements <see cref="ICollectionTrackingService"/> by forwarding calls to a remote
/// <c>MtgCollectionTracker.Server</c> instance via the strongly-typed OpenAPI-generated
/// <see cref="IMtgCollectionTrackerApiClient"/>.
/// </summary>
public class RemoteCollectionTrackingService : ICollectionTrackingService
{
    private readonly HttpClient _http;
    private readonly Gen.IMtgCollectionTrackerApiClient _client;

    // Web defaults (camelCase + case-insensitive) match both ASP.NET Core's default
    // JSON output and the [JsonPropertyName] attributes on the generated types.
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    /// <param name="http">
    /// Pre-configured <see cref="HttpClient"/> with <see cref="HttpClient.BaseAddress"/>
    /// pointing at the server and an <c>X-Api-Key</c> default request header if the server
    /// requires authentication.
    /// </param>
    public RemoteCollectionTrackingService(HttpClient http)
    {
        _http = http;
        // The generated client reuses the same HttpClient so BaseAddress / headers are shared.
        _client = new Gen.MtgCollectionTrackerApiClient(http);
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Maps a generated-client type to a Core-model type via a JSON round-trip.
    /// Both use camelCase JSON so the conversion is always safe.
    /// </summary>
    private static T Map<T>(object source)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(source, JsonOpts);
        return JsonSerializer.Deserialize<T>(bytes, JsonOpts)!;
    }

    /// <summary>Maps a collection of generated-client types to Core-model types.</summary>
    private static T[] MapMany<T>(System.Collections.IEnumerable? source)
    {
        if (source is null) return [];
        var bytes = JsonSerializer.SerializeToUtf8Bytes(source, JsonOpts);
        return JsonSerializer.Deserialize<T[]>(bytes, JsonOpts) ?? [];
    }

    // ── SSE helper ────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a long-running SSE operation, streams progress events, and returns the
    /// JSON-encoded payload from the final "done" event (if any).
    /// </summary>
    private async ValueTask<string?> StreamSseAsync(
        string startUrl,
        object? startBody,
        UpdateCardMetadataProgressCallback? callback,
        CancellationToken cancel)
    {
        HttpResponseMessage startResp;
        if (startBody is null)
            startResp = await _http.PostAsync(startUrl, content: null, cancel);
        else
            startResp = await _http.PostAsJsonAsync(startUrl, startBody, JsonOpts, cancel);

        startResp.EnsureSuccessStatusCode();

        var started = await startResp.Content
            .ReadFromJsonAsync<OperationStartedDto>(JsonOpts, cancel);
        var operationId = started!.OperationId;

        using var sseResp = await _http.GetAsync(
            $"/api/operations/{operationId}/events",
            HttpCompletionOption.ResponseHeadersRead,
            cancel);
        sseResp.EnsureSuccessStatusCode();

        using var stream = await sseResp.Content.ReadAsStreamAsync(cancel);
        using var reader = new StreamReader(stream);

        while (!cancel.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancel);
            if (line is null) break;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<ProgressEventDto>(json, JsonOpts);
            if (evt is null) continue;

            switch (evt.Type)
            {
                case "progress":
                    callback?.OnProgress?.Invoke(evt.Current, evt.Total);
                    break;
                case "download":
                    callback?.OnDownloadStatus?.Invoke(evt.Message ?? string.Empty);
                    break;
                case "done":
                    return evt.Payload;
                case "error":
                    throw new InvalidOperationException(evt.Message ?? "Remote operation failed");
            }
        }

        return null;
    }

    // ── Languages ─────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<CardLanguageModel>> GetLanguagesAsync(CancellationToken cancel)
        => MapMany<CardLanguageModel>(await _client.LanguagesAsync(cancel));

    // ── Cards ─────────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<CardSkuModel>> GetCardsAsync(CardQueryModel query, CancellationToken cancel)
        => MapMany<CardSkuModel>(
            await _client.CardsAllAsync(
                searchFilter: string.IsNullOrWhiteSpace(query.SearchFilter) ? null : query.SearchFilter,
                notInDecks: query.NotInDecks ? true : null,
                noProxies: query.NoProxies ? true : null,
                unParented: query.UnParented ? true : null,
                missingMetadata: query.MissingMetadata ? true : null,
                includeScryfallMetadata: query.IncludeScryfallMetadata ? true : null,
                deckIds: query.DeckIds,
                containerIds: query.ContainerIds,
                tags: query.Tags,
                colors: query.Colors,
                cardTypes: query.CardTypes,
                cancellationToken: cancel));

    public async ValueTask<CardSkuModel> GetCardSkuByIdAsync(Guid id, CancellationToken cancel)
        => Map<CardSkuModel>(await _client.CardsGETAsync(id, cancel));

    public async ValueTask<PaginatedCardSkuModel> GetCardsForContainerAsync(int containerId, FetchContainerPageModel options, CancellationToken cancel)
        => Map<PaginatedCardSkuModel>(
            await _client.ContainerAsync(
                containerId,
                options.PageNumber,
                options.PageSize,
                options.ShowOnlyMissingMetadata ? true : null,
                cancel)!);

    public async ValueTask<bool> IsBasicLandAsync(string cardName, CancellationToken cancel)
        => await _client.IsBasicLandAsync(cardName, cancel);

    public async ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model, CancellationToken cancel)
        => Map<CardSkuModel>(await _client.AddToDeckAsync(Map<Gen.AddToDeckInputModel>(model), cancel));

    public async ValueTask<CardSkuModel> AddToDeckOrContainerAsync(
        int? containerId, int? deckId, AddToDeckOrContainerInputModel model, CancellationToken cancel)
        => Map<CardSkuModel>(await _client.AddAsync(Map<Gen.AddToDeckOrContainerInputModel>(model), containerId, deckId, cancel));

    public async ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(
        int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items,
        IScryfallApiClient? scryfallClient, CancellationToken cancel)
    {
        var batch = new Gen.AddBatchRequest();
        batch.ContainerId = containerId;
        batch.DeckId = deckId;
        batch.Items = items.Select(i => Map<Gen.AddToDeckOrContainerInputModel>(i)).ToList();
        var result = await _client.AddBatchAsync(batch, cancel);
        return (result.Total, result.ProxyTotal, result.Rows);
    }

    public async ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(
        UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
        => Map<UpdateCardSkuResult>(await _client.CardsPUTAsync(model.Ids.First(), Map<Gen.UpdateCardSkuInputModel>(model), cancel));

    public async ValueTask<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model, CancellationToken cancel)
        => Map<CardSkuModel>(await _client.SplitAsync(model.CardSkuId, Map<Gen.SplitCardSkuInputModel>(model), cancel));

    public async ValueTask<CardSkuModel> DeleteCardSkuAsync(Guid skuId, CancellationToken cancel)
        => Map<CardSkuModel>(await _client.DeleteAsync(skuId, cancel));

    public async ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(
        RemoveFromDeckInputModel model, CancellationToken cancel)
    {
        var result = await _client.RemoveFromDeckAsync(Map<Gen.RemoveFromDeckInputModel>(model), cancel);
        return (Map<CardSkuModel>(result.Sku), result.WasMerged);
    }

    public async ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(
        ConsolidateCardSkusInputModel model, CancellationToken cancel)
    {
        var result = await _client.ConsolidateAsync(Map<Gen.ConsolidateCardSkusInputModel>(model), cancel);
        return (result.SkusUpdated, result.SkusRemoved);
    }

    public async ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(
        IEnumerable<string> cardNames, IScryfallApiClient client, CancellationToken cancel)
    {
        var result = await _client.ResolveEditionsAsync(cardNames.ToArray(), cancel);
        // Map each value from the generated ScryfallResolvedCard to the ScryfallApi.Client version.
        var dict = new Dictionary<string, ScryfallResolvedCard>(result.Count);
        foreach (var kv in result)
            dict[kv.Key] = Map<ScryfallResolvedCard>(kv.Value!);
        return dict;
    }

    // ── Containers ────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<ContainerSummaryModel>> GetContainersAsync(CancellationToken cancel)
        => MapMany<ContainerSummaryModel>(await _client.ContainersAllAsync(cancel));

    public async ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description, CancellationToken cancel)
        => Map<ContainerSummaryModel>(await _client.ContainersPOSTAsync(new Gen.CreateContainerRequest { Name = name, Description = description }, cancel));

    public async ValueTask<ContainerSummaryModel> UpdateContainerAsync(
        int id, string name, string? description, CancellationToken cancel)
        => Map<ContainerSummaryModel>(await _client.ContainersPUTAsync(id, new Gen.CreateContainerRequest { Name = name, Description = description }, cancel));

    public async ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model, CancellationToken cancel)
        => Map<DeleteContainerResult>(await _client.Delete2Async(model.ContainerId, Map<Gen.DeleteContainerInputModel>(model), cancel));

    public async ValueTask<string> PrintContainerAsync(int containerId, ContainerPrintOptions options, CancellationToken cancel)
    {
        var reportProxyUsage = options.ReportProxyUsage ? "true" : "false";
        return await _http.GetStringAsync($"/api/containers/{containerId}/print?reportProxyUsage={reportProxyUsage}", cancel);
    }

    // ── Decks ─────────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<DeckSummaryModel>> GetDecksAsync(DeckFilterModel? filter, CancellationToken cancel)
        => MapMany<DeckSummaryModel>(
            await _client.DecksAllAsync(formats: filter?.Formats, deckIds: filter?.Ids, cancellationToken: cancel));

    public async ValueTask<DeckModel> GetDeckAsync(
        int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
        => Map<DeckModel>(await _client.DecksGETAsync(deckId, cancel));

    public async ValueTask<DeckSummaryModel> CreateDeckAsync(
        string name, string? format, int? containerId, bool isCommander, CancellationToken cancel)
        => Map<DeckSummaryModel>(await _client.DecksPOSTAsync(new Gen.CreateDeckRequest { Name = name, Format = format, ContainerId = containerId, IsCommander = isCommander }, cancel));

    public async ValueTask<DeckSummaryModel> UpdateDeckAsync(
        int id, string name, string? format, int? containerId, bool isCommander, CancellationToken cancel)
        => Map<DeckSummaryModel>(await _client.DecksPUTAsync(id, new Gen.CreateDeckRequest { Name = name, Format = format, ContainerId = containerId, IsCommander = isCommander }, cancel));

    public async ValueTask<DeckSummaryModel> SetDeckBannerAsync(int deckId, Guid? cardSkuId, CancellationToken cancel)
        => Map<DeckSummaryModel>(await _client.BannerAsync(deckId, new Gen.SetBannerRequest { CardSkuId = cardSkuId }, cancel));

    public async ValueTask<DeckSummaryModel> SetDeckCommanderAsync(int deckId, Guid? commanderSkuId, CancellationToken cancel)
        => Map<DeckSummaryModel>(await _client.CommanderAsync(deckId, new Gen.SetCommanderRequest { CommanderSkuId = commanderSkuId }, cancel));

    public async ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model, CancellationToken cancel)
        => Map<DismantleDeckResult>(await _client.DismantleAsync(model.DeckId, Map<Gen.DismantleDeckInputModel>(model), cancel));

    public async ValueTask<CommanderValidationResult> ValidateCommanderDeckAsync(
        int deckId, CancellationToken cancel)
        => Map<CommanderValidationResult>(await _client.ValidateAsync(deckId, cancel));

    public async ValueTask<IReadOnlyList<string>> GetDeckFormatsAsync(CancellationToken cancel)
        => (await _client.FormatsAsync(cancel))?.ToArray() ?? [];

    public async ValueTask<bool> HasOtherDecksInFormatAsync(string format, CancellationToken cancel)
        => await _client.HasOtherFormatsAsync(format, cancel);

    public async ValueTask<string> PrintDeckAsync(int deckId, DeckPrintOptions options, CancellationToken cancel)
    {
        var reportProxyUsage = options.ReportProxyUsage ? "true" : "false";
        return await _http.GetStringAsync($"/api/decks/{deckId}/print?reportProxyUsage={reportProxyUsage}", cancel);
    }

    // ── Wishlist ──────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<WishlistItemModel>> GetWishlistItemsAsync(WishlistItemFilter filter, CancellationToken cancel)
        => MapMany<WishlistItemModel>(await _client.WishlistAllGETAsync(filter.Tags, cancel));

    public async ValueTask<IReadOnlyList<WishlistItemModel>> AddMultipleToWishlistAsync(
        IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient, CancellationToken cancel)
        => MapMany<WishlistItemModel>(await _client.WishlistAllPOSTAsync(items.Select(i => Map<Gen.AddToWishlistInputModel>(i)).ToArray(), cancel));

    public async ValueTask<WishlistItemModel> UpdateWishlistItemAsync(
        UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
        => Map<WishlistItemModel>(await _client.WishlistAsync(model.Id, Map<Gen.UpdateWishlistItemInputModel>(model), cancel));

    public async ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id, CancellationToken cancel)
        => Map<WishlistItemModel>(await _client.Delete3Async(id, cancel));

    public async ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(
        MoveWishlistItemsToCollectionInputModel model, CancellationToken cancel)
        => Map<MoveWishlistItemsToCollectionResult>(await _client.MoveToCollectionAsync(Map<Gen.MoveWishlistItemsToCollectionInputModel>(model), cancel));

    public async ValueTask<WishlistSpendSummaryModel> GetWishlistSpendAsync(CancellationToken cancel)
        => Map<WishlistSpendSummaryModel>(await _client.SpendAsync(cancel)!);

    public async ValueTask<WishlistBuyingListModel> GenerateBuyingListAsync(CancellationToken cancel)
    {
        var entries = MapMany<BuyingListVendorEntry>(
            await _client.BuyingListAsync(cancel));
        var model = new WishlistBuyingListModel();
        foreach (var entry in entries)
            foreach (var item in entry.Items)
                model.Add(entry.Vendor, item);
        return model;
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<NotesModel>> GetNotesAsync(CancellationToken cancel)
        => MapMany<NotesModel>(await _client.NotesAllAsync(cancel));

    public async ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes, CancellationToken cancel)
        => Map<NotesModel>(await _client.NotesPOSTAsync(new Gen.UpdateNotesRequest { Id = id, Title = title, Notes = notes }, cancel));

    public async ValueTask<bool> DeleteNotesAsync(int id, CancellationToken cancel)
        => await _client.NotesDELETEAsync(id, cancel);

    // ── Vendors ───────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<VendorModel>> GetVendorsAsync(CancellationToken cancel)
        => MapMany<VendorModel>(await _client.VendorsAllAsync(cancel));

    public async ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model, CancellationToken cancel)
    {
        var result = await _client.VendorsAsync(Map<Gen.ApplyVendorsInputModel>(model), cancel);
        return (result.Created, result.Deleted);
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    public async ValueTask<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancel)
        => (await _client.TagsAllAsync(cancel))?.ToArray() ?? [];

    public async ValueTask<ApplyTagsResult> ApplyTagsAsync(
        IEnumerable<string> tags, CancellationToken cancel)
        => Map<ApplyTagsResult>(await _client.TagsAsync(tags.ToArray(), cancel));

    // ── Collection ────────────────────────────────────────────────────────────

    public async ValueTask<CollectionSummaryModel> GetCollectionSummaryAsync(CancellationToken cancel)
        => Map<CollectionSummaryModel>(await _client.SummaryAsync(cancel)!);

    public async ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(
        string cardName, int wantQty, bool noProxies, bool sparesOnly, CancellationToken cancel)
        => Map<CheckQuantityResult>(await _client.CheckQuantityAsync(cardName, wantQty, noProxies, sparesOnly, cancel));

    // ── Prices ────────────────────────────────────────────────────────────────

    public async ValueTask<(double? price, string? provider)> GetLatestPriceForSkuAsync(
        Guid skuId, string currency, CancellationToken cancel)
    {
        var result = await _client.SkuAsync(skuId, currency, cancel);
        return (result?.Price, result?.Provider);
    }

    public async ValueTask<CardPriceHistoryModel?> GetPriceHistoryForSkuAsync(
        Guid skuId, string currency, CancellationToken cancel)
    {
        var result = await _client.HistoryAsync(skuId, currency, cancel);
        return result is null ? null : Map<CardPriceHistoryModel>(result);
    }

    public async ValueTask<List<LowestPriceCheckItem>> GetLowestPricesAsync(
        LowestPriceCheckOptions options, IScryfallApiClient client, CancellationToken cancel)
        => MapMany<LowestPriceCheckItem>(await _client.LowestAsync(Map<Gen.LowestPriceCheckOptions>(options), cancel)).ToList();

    // ── Card images ───────────────────────────────────────────────────────────

    private async ValueTask<Stream?> GetImageAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            requestUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var imageStream = MemoryStreamPool.GetStream(nameof(RemoteCollectionTrackingService));
        await responseStream.CopyToAsync(imageStream, cancellationToken);
        imageStream.Position = 0;
        return imageStream;
    }

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/sku/{cardSkuId}/front/large", cancel);

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/sku/{cardSkuId}/front/small", cancel);

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/sku/{cardSkuId}/back/large", cancel);

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(Guid cardSkuId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/sku/{cardSkuId}/back/small", cancel);

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(int wishlistItemId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/wishlist/{wishlistItemId}/front/large", cancel);

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(int wishlistItemId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/wishlist/{wishlistItemId}/front/small", cancel);

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(int wishlistItemId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/wishlist/{wishlistItemId}/back/large", cancel);

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(int wishlistItemId, CancellationToken cancel)
        => await GetImageAsync($"/api/images/wishlist/{wishlistItemId}/back/small", cancel);

    // ── Scryfall identifiers ──────────────────────────────────────────────────

    public async ValueTask<bool> IsScryfallIdMappingEmptyAsync(CancellationToken cancel)
        => await _client.IsEmptyAsync(cancel);

    public async ValueTask ImportCardIdentifiersAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
        => await StreamSseAsync("/api/identifiers/import", null, callback, cancel);

    // ── Metadata operations (long-running, SSE) ───────────────────────────────

    public async ValueTask<IReadOnlyList<CardSkuModel>> UpdateCardMetadataAsync(
        ICollection<Guid> ids,
        IScryfallApiClient scryfallApiClient,
        UpdateCardMetadataProgressCallback? callback,
        CancellationToken cancel)
    {
        var payload = await StreamSseAsync(
            "/api/metadata/updateCards",
            new Gen.UpdateCardsMetadataRequest { Ids = ids.ToList() },
            callback,
            cancel);

        return string.IsNullOrEmpty(payload)
            ? []
            : JsonSerializer.Deserialize<CardSkuModel[]>(payload, JsonOpts) ?? [];
    }

    public async ValueTask<IReadOnlyList<WishlistItemModel>> UpdateWishlistMetadataAsync(
        ICollection<int> ids,
        IScryfallApiClient scryfallApiClient,
        UpdateCardMetadataProgressCallback? callback,
        CancellationToken cancel)
    {
        var payload = await StreamSseAsync(
            "/api/metadata/updateWishlist",
            new Gen.UpdateWishlistMetadataRequest { Ids = ids.ToList() },
            callback,
            cancel);

        return string.IsNullOrEmpty(payload)
            ? []
            : JsonSerializer.Deserialize<WishlistItemModel[]>(payload, JsonOpts) ?? [];
    }

    public async ValueTask AddMissingMetadataAsync(
        UpdateCardMetadataProgressCallback callback,
        IScryfallApiClient scryfallApiClient,
        CancellationToken cancel)
        => await StreamSseAsync("/api/metadata/addMissing", null, callback, cancel);

    public async ValueTask RebuildAllMetadataAsync(
        UpdateCardMetadataProgressCallback callback,
        IScryfallApiClient scryfallApiClient,
        CancellationToken cancel)
        => await StreamSseAsync("/api/metadata/rebuildAll", null, callback, cancel);

    public async ValueTask NormalizeCardNamesAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
        => await StreamSseAsync("/api/metadata/normalizeNames", null, callback, cancel);

    public async ValueTask<bool> ImportPriceDataAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        var payload = await StreamSseAsync("/api/prices/import", null, callback, cancel);
        return !string.IsNullOrEmpty(payload)
            && JsonSerializer.Deserialize<bool>(payload, JsonOpts);
    }
}
