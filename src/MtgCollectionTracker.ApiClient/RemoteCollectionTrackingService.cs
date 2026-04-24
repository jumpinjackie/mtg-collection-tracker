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

    public IEnumerable<CardLanguageModel> GetLanguages()
        => MapMany<CardLanguageModel>(
            _client.LanguagesAsync().ConfigureAwait(false).GetAwaiter().GetResult());

    // ── Cards ─────────────────────────────────────────────────────────────────

    public IEnumerable<CardSkuModel> GetCards(CardQueryModel query)
        => MapMany<CardSkuModel>(
            _client.CardsAllAsync(
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
                cardTypes: query.CardTypes)
            .ConfigureAwait(false).GetAwaiter().GetResult());

    public async ValueTask<CardSkuModel> GetCardSkuByIdAsync(Guid id, CancellationToken cancel)
        => Map<CardSkuModel>(await _client.CardsGETAsync(id, cancel));

    public PaginatedCardSkuModel GetCardsForContainer(int containerId, FetchContainerPageModel options)
        => Map<PaginatedCardSkuModel>(
            _client.ContainerAsync(
                containerId,
                options.PageNumber,
                options.PageSize,
                options.ShowOnlyMissingMetadata ? true : null)
            .ConfigureAwait(false).GetAwaiter().GetResult()!);

    public bool IsBasicLand(string cardName)
        => _client.IsBasicLandAsync(cardName).ConfigureAwait(false).GetAwaiter().GetResult();

    public async ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model)
        => Map<CardSkuModel>(await _client.AddToDeckAsync(Map<Gen.AddToDeckInputModel>(model)));

    public async ValueTask<CardSkuModel> AddToDeckOrContainerAsync(
        int? containerId, int? deckId, AddToDeckOrContainerInputModel model)
        => Map<CardSkuModel>(await _client.AddAsync(Map<Gen.AddToDeckOrContainerInputModel>(model), containerId, deckId));

    public async ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(
        int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items,
        IScryfallApiClient? scryfallClient)
    {
        var batch = new Gen.AddBatchRequest();
        batch.ContainerId = containerId;
        batch.DeckId = deckId;
        batch.Items = items.Select(i => Map<Gen.AddToDeckOrContainerInputModel>(i)).ToList();
        var result = await _client.AddBatchAsync(batch);
        return (result.Total, result.ProxyTotal, result.Rows);
    }

    public async ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(
        UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
        => Map<UpdateCardSkuResult>(await _client.CardsPUTAsync(model.Ids.First(), Map<Gen.UpdateCardSkuInputModel>(model), cancel));

    public async Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model)
        => Map<CardSkuModel>(await _client.SplitAsync(model.CardSkuId, Map<Gen.SplitCardSkuInputModel>(model)));

    public async ValueTask<CardSkuModel> DeleteCardSkuAsync(Guid skuId)
        => Map<CardSkuModel>(await _client.DeleteAsync(skuId));

    public async ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(
        RemoveFromDeckInputModel model)
    {
        var result = await _client.RemoveFromDeckAsync(Map<Gen.RemoveFromDeckInputModel>(model));
        return (Map<CardSkuModel>(result.Sku), result.WasMerged);
    }

    public async ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(
        ConsolidateCardSkusInputModel model)
    {
        var result = await _client.ConsolidateAsync(Map<Gen.ConsolidateCardSkusInputModel>(model));
        return (result.SkusUpdated, result.SkusRemoved);
    }

    public async ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(
        IEnumerable<string> cardNames, IScryfallApiClient client)
    {
        var result = await _client.ResolveEditionsAsync(cardNames.ToArray());
        // Map each value from the generated ScryfallResolvedCard to the ScryfallApi.Client version.
        var dict = new Dictionary<string, ScryfallResolvedCard>(result.Count);
        foreach (var kv in result)
            dict[kv.Key] = Map<ScryfallResolvedCard>(kv.Value!);
        return dict;
    }

    // ── Containers ────────────────────────────────────────────────────────────

    public IEnumerable<ContainerSummaryModel> GetContainers()
        => MapMany<ContainerSummaryModel>(
            _client.ContainersAllAsync().ConfigureAwait(false).GetAwaiter().GetResult());

    public async ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description)
        => Map<ContainerSummaryModel>(await _client.ContainersPOSTAsync(new Gen.CreateContainerRequest { Name = name, Description = description }));

    public async ValueTask<ContainerSummaryModel> UpdateContainerAsync(
        int id, string name, string? description)
        => Map<ContainerSummaryModel>(await _client.ContainersPUTAsync(id, new Gen.CreateContainerRequest { Name = name, Description = description }));

    public async ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model)
        => Map<DeleteContainerResult>(await _client.Delete2Async(model.ContainerId, Map<Gen.DeleteContainerInputModel>(model)));

    public string PrintContainer(int containerId, ContainerPrintOptions options)
    {
        var qs = options.ReportProxyUsage ? "?reportProxyUsage=true" : string.Empty;
        return _http.GetStringAsync($"/api/containers/{containerId}/print{qs}")
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    // ── Decks ─────────────────────────────────────────────────────────────────

    public IEnumerable<DeckSummaryModel> GetDecks(DeckFilterModel? filter)
        => MapMany<DeckSummaryModel>(
            _client.DecksAllAsync(formats: filter?.Formats, deckIds: filter?.Ids)
            .ConfigureAwait(false).GetAwaiter().GetResult());

    public async ValueTask<DeckModel> GetDeckAsync(
        int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
        => Map<DeckModel>(await _client.DecksGETAsync(deckId, cancel));

    public async ValueTask<DeckSummaryModel> CreateDeckAsync(
        string name, string? format, int? containerId, bool isCommander = false)
        => Map<DeckSummaryModel>(await _client.DecksPOSTAsync(new Gen.CreateDeckRequest { Name = name, Format = format, ContainerId = containerId, IsCommander = isCommander }));

    public async ValueTask<DeckSummaryModel> UpdateDeckAsync(
        int id, string name, string? format, int? containerId, bool isCommander = false)
        => Map<DeckSummaryModel>(await _client.DecksPUTAsync(id, new Gen.CreateDeckRequest { Name = name, Format = format, ContainerId = containerId, IsCommander = isCommander }));

    public async ValueTask<DeckSummaryModel> SetDeckBannerAsync(int deckId, Guid? cardSkuId)
        => Map<DeckSummaryModel>(await _client.BannerAsync(deckId, new Gen.SetBannerRequest { CardSkuId = cardSkuId }));

    public async ValueTask<DeckSummaryModel> SetDeckCommanderAsync(int deckId, Guid? commanderSkuId)
        => Map<DeckSummaryModel>(await _client.CommanderAsync(deckId, new Gen.SetCommanderRequest { CommanderSkuId = commanderSkuId }));

    public async ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model)
        => Map<DismantleDeckResult>(await _client.DismantleAsync(model.DeckId, Map<Gen.DismantleDeckInputModel>(model)));

    public async ValueTask<CommanderValidationResult> ValidateCommanderDeckAsync(
        int deckId, CancellationToken cancel)
        => Map<CommanderValidationResult>(await _client.ValidateAsync(deckId, cancel));

    public IEnumerable<string> GetDeckFormats()
        => _client.FormatsAsync().ConfigureAwait(false).GetAwaiter().GetResult() ?? [];

    public bool HasOtherDecksInFormat(string format)
        => _client.HasOtherFormatsAsync(format).ConfigureAwait(false).GetAwaiter().GetResult();

    public string PrintDeck(int deckId, DeckPrintOptions options)
    {
        var qs = options.ReportProxyUsage ? "?reportProxyUsage=true" : string.Empty;
        return _http.GetStringAsync($"/api/decks/{deckId}/print{qs}")
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    // ── Wishlist ──────────────────────────────────────────────────────────────

    public IEnumerable<WishlistItemModel> GetWishlistItems(WishlistItemFilter filter)
        => MapMany<WishlistItemModel>(
            _client.WishlistAllGETAsync(filter.Tags)
            .ConfigureAwait(false).GetAwaiter().GetResult());

    public async ValueTask<ICollection<WishlistItemModel>> AddMultipleToWishlistAsync(
        IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient)
        => MapMany<WishlistItemModel>(await _client.WishlistAllPOSTAsync(items.Select(i => Map<Gen.AddToWishlistInputModel>(i)).ToArray()));

    public async ValueTask<WishlistItemModel> UpdateWishlistItemAsync(
        UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
        => Map<WishlistItemModel>(await _client.WishlistAsync(model.Id, Map<Gen.UpdateWishlistItemInputModel>(model), cancel));

    public async ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id)
        => Map<WishlistItemModel>(await _client.Delete3Async(id));

    public async ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(
        MoveWishlistItemsToCollectionInputModel model)
        => Map<MoveWishlistItemsToCollectionResult>(await _client.MoveToCollectionAsync(Map<Gen.MoveWishlistItemsToCollectionInputModel>(model)));

    public WishlistSpendSummaryModel GetWishlistSpend()
        => Map<WishlistSpendSummaryModel>(
            _client.SpendAsync().ConfigureAwait(false).GetAwaiter().GetResult()!);

    public WishlistBuyingListModel GenerateBuyingList()
    {
        var entries = MapMany<BuyingListVendorEntry>(
            _client.BuyingListAsync().ConfigureAwait(false).GetAwaiter().GetResult());
        var model = new WishlistBuyingListModel();
        foreach (var entry in entries)
            foreach (var item in entry.Items)
                model.Add(entry.Vendor, item);
        return model;
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    public IEnumerable<NotesModel> GetNotes()
        => MapMany<NotesModel>(
            _client.NotesAllAsync().ConfigureAwait(false).GetAwaiter().GetResult());

    public async ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes)
        => Map<NotesModel>(await _client.NotesPOSTAsync(new Gen.UpdateNotesRequest { Id = id, Title = title, Notes = notes }));

    public async ValueTask<bool> DeleteNotesAsync(int id)
        => await _client.NotesDELETEAsync(id);

    // ── Vendors ───────────────────────────────────────────────────────────────

    public IEnumerable<VendorModel> GetVendors()
        => MapMany<VendorModel>(
            _client.VendorsAllAsync().ConfigureAwait(false).GetAwaiter().GetResult());

    public async ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model)
    {
        var result = await _client.VendorsAsync(Map<Gen.ApplyVendorsInputModel>(model));
        return (result.Created, result.Deleted);
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    public IEnumerable<string> GetTags()
        => _client.TagsAllAsync().ConfigureAwait(false).GetAwaiter().GetResult() ?? [];

    public async ValueTask<ApplyTagsResult> ApplyTagsAsync(
        IEnumerable<string> tags, CancellationToken cancel)
        => Map<ApplyTagsResult>(await _client.TagsAsync(tags.ToArray(), cancel));

    // ── Collection ────────────────────────────────────────────────────────────

    public CollectionSummaryModel GetCollectionSummary()
        => Map<CollectionSummaryModel>(
            _client.SummaryAsync().ConfigureAwait(false).GetAwaiter().GetResult()!);

    public async ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(
        string cardName, int wantQty, bool noProxies, bool sparesOnly)
        => Map<CheckQuantityResult>(await _client.CheckQuantityAsync(cardName, wantQty, noProxies, sparesOnly));

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

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/front/large", HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStreamAsync() : null;
        }
        catch { return null; }
    }

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/front/small", HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStreamAsync() : null;
        }
        catch { return null; }
    }

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/back/large", HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStreamAsync() : null;
        }
        catch { return null; }
    }

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/back/small", HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStreamAsync() : null;
        }
        catch { return null; }
    }

    // ── Scryfall identifiers ──────────────────────────────────────────────────

    public async ValueTask<bool> IsScryfallIdMappingEmptyAsync(CancellationToken cancel)
        => await _client.IsEmptyAsync(cancel);

    public async ValueTask ImportCardIdentifiersAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
        => await StreamSseAsync("/api/identifiers/import", null, callback, cancel);

    // ── Metadata operations (long-running, SSE) ───────────────────────────────

    public async ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(
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

    public async ValueTask<IEnumerable<WishlistItemModel>> UpdateWishlistMetadataAsync(
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

