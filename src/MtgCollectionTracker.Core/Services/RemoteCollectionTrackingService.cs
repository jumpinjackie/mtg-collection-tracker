using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MtgCollectionTracker.Core.Model;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Core.Services;

// ── Internal DTOs ─────────────────────────────────────────────────────────────

internal record OperationStartedResponse(
    [property: JsonPropertyName("operationId")] string OperationId);

internal record AddMultipleResult(int Total, int ProxyTotal, int Rows);

internal record ConsolidateResult(int SkusUpdated, int SkusRemoved);

internal record VendorApplyResult(int Created, int Deleted);

internal record RemoveFromDeckResult(CardSkuModel Sku, bool WasMerged);

internal record SkuPriceResult(double? Price, string? Provider);

internal record BuyingListVendorEntry(string Vendor, BuyingListItem[] Items);

internal class ProgressEventDto
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

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Implements <see cref="ICollectionTrackingService"/> by forwarding calls to a remote
/// <c>MtgCollectionTracker.Server</c> instance over HTTP.
/// </summary>
public class RemoteCollectionTrackingService : ICollectionTrackingService
{
    private readonly HttpClient _http;

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
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildQueryString(IEnumerable<(string Key, string? Value)> pairs)
    {
        var parts = pairs
            .Where(p => p.Value != null)
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : string.Empty;
    }

    /// <summary>
    /// Starts a long-running SSE operation, streams events, calls the progress callback,
    /// and returns the deserialized payload from the "done" event (if any).
    /// </summary>
    private async ValueTask<string?> StreamSseAsync(
        string startUrl,
        object? startBody,
        UpdateCardMetadataProgressCallback? callback,
        CancellationToken cancel)
    {
        // Kick off the operation.
        HttpResponseMessage startResp;
        if (startBody is null)
            startResp = await _http.PostAsync(startUrl, content: null, cancel);
        else
            startResp = await _http.PostAsJsonAsync(startUrl, startBody, JsonOpts, cancel);

        startResp.EnsureSuccessStatusCode();

        var started = await startResp.Content
            .ReadFromJsonAsync<OperationStartedResponse>(JsonOpts, cancel);
        var operationId = started!.OperationId;

        // Connect to the SSE stream.
        using var sseResp = await _http.GetAsync(
            $"/api/operations/{operationId}/events",
            HttpCompletionOption.ResponseHeadersRead,
            cancel);
        sseResp.EnsureSuccessStatusCode();

        using var stream = await sseResp.Content.ReadAsStreamAsync(cancel);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancel.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancel);
            if (line is null)
                break;
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var json = line["data: ".Length..];
            var evt = JsonSerializer.Deserialize<ProgressEventDto>(json, JsonOpts);
            if (evt is null)
                continue;

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
                    throw new InvalidOperationException(
                        evt.Message ?? "Remote operation failed");
            }
        }

        return null;
    }

    // ── Languages ─────────────────────────────────────────────────────────────

    public IEnumerable<CardLanguageModel> GetLanguages()
    {
        return _http.GetFromJsonAsync<List<CardLanguageModel>>("/api/languages", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    // ── Cards ─────────────────────────────────────────────────────────────────

    public IEnumerable<CardSkuModel> GetCards(CardQueryModel query)
    {
        var qs = new List<(string, string?)>();
        if (query.SearchFilter != null) qs.Add(("searchFilter", query.SearchFilter));
        if (query.NotInDecks) qs.Add(("notInDecks", "true"));
        if (query.NoProxies) qs.Add(("noProxies", "true"));
        if (query.UnParented) qs.Add(("unParented", "true"));
        if (query.MissingMetadata) qs.Add(("missingMetadata", "true"));
        if (query.IncludeScryfallMetadata) qs.Add(("includeScryfallMetadata", "true"));
        if (query.DeckIds?.Length > 0) qs.Add(("deckIds", string.Join(",", query.DeckIds)));
        if (query.ContainerIds?.Length > 0) qs.Add(("containerIds", string.Join(",", query.ContainerIds)));
        if (query.Tags != null) qs.Add(("tags", string.Join(",", query.Tags)));
        if (query.Colors != null) qs.Add(("colors", string.Join(",", query.Colors)));
        if (query.CardTypes != null) qs.Add(("cardTypes", string.Join(",", query.CardTypes)));

        var url = "/api/cards" + BuildQueryString(qs);
        return _http.GetFromJsonAsync<List<CardSkuModel>>(url, JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<CardSkuModel> GetCardSkuByIdAsync(Guid id, CancellationToken cancel)
    {
        var result = await _http.GetFromJsonAsync<CardSkuModel>($"/api/cards/{id}", JsonOpts, cancel);
        return result!;
    }

    public PaginatedCardSkuModel GetCardsForContainer(int containerId, FetchContainerPageModel options)
    {
        var qs = new List<(string, string?)>
        {
            ("page", options.PageNumber.ToString()),
        };
        if (options.PageSize.HasValue) qs.Add(("pageSize", options.PageSize.Value.ToString()));
        if (options.ShowOnlyMissingMetadata) qs.Add(("missingMetadata", "true"));

        var url = $"/api/cards/container/{containerId}" + BuildQueryString(qs);
        var result = _http.GetFromJsonAsync<PaginatedCardSkuModel>(url, JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        return result!;
    }

    public bool IsBasicLand(string cardName)
    {
        return _http.GetFromJsonAsync<bool>(
            $"/api/cards/isBasicLand/{Uri.EscapeDataString(cardName)}", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async ValueTask<CardSkuModel> AddToDeckAsync(AddToDeckInputModel model)
    {
        var resp = await _http.PostAsJsonAsync("/api/cards/addToDeck", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CardSkuModel>(JsonOpts))!;
    }

    public async ValueTask<CardSkuModel> AddToDeckOrContainerAsync(
        int? containerId, int? deckId, AddToDeckOrContainerInputModel model)
    {
        var qs = new List<(string, string?)>();
        if (containerId.HasValue) qs.Add(("containerId", containerId.Value.ToString()));
        if (deckId.HasValue) qs.Add(("deckId", deckId.Value.ToString()));

        var url = "/api/cards/add" + BuildQueryString(qs);
        var resp = await _http.PostAsJsonAsync(url, model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CardSkuModel>(JsonOpts))!;
    }

    public async ValueTask<(int total, int proxyTotal, int rows)> AddMultipleToContainerOrDeckAsync(
        int? containerId, int? deckId, IEnumerable<AddToDeckOrContainerInputModel> items,
        IScryfallApiClient? scryfallClient)
    {
        var body = new { containerId, deckId, items };
        var resp = await _http.PostAsJsonAsync("/api/cards/addBatch", body, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<AddMultipleResult>(JsonOpts);
        return (dto!.Total, dto.ProxyTotal, dto.Rows);
    }

    public async ValueTask<UpdateCardSkuResult> UpdateCardSkuAsync(
        UpdateCardSkuInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        var id = model.Ids.First();
        var resp = await _http.PutAsJsonAsync($"/api/cards/{id}", model, JsonOpts, cancel);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<UpdateCardSkuResult>(JsonOpts, cancel))!;
    }

    public async Task<CardSkuModel> SplitCardSkuAsync(SplitCardSkuInputModel model)
    {
        var resp = await _http.PostAsJsonAsync($"/api/cards/{model.CardSkuId}/split", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CardSkuModel>(JsonOpts))!;
    }

    public async ValueTask<CardSkuModel> DeleteCardSkuAsync(Guid skuId)
    {
        var resp = await _http.PostAsync($"/api/cards/{skuId}/delete", content: null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CardSkuModel>(JsonOpts))!;
    }

    public async ValueTask<(CardSkuModel sku, bool wasMerged)> RemoveFromDeckAsync(
        RemoveFromDeckInputModel model)
    {
        var resp = await _http.PostAsJsonAsync("/api/cards/removeFromDeck", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<RemoveFromDeckResult>(JsonOpts);
        return (dto!.Sku, dto.WasMerged);
    }

    public async ValueTask<(int skusUpdated, int skusRemoved)> ConsolidateCardSkusAsync(
        ConsolidateCardSkusInputModel model)
    {
        var resp = await _http.PostAsJsonAsync("/api/cards/consolidate", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<ConsolidateResult>(JsonOpts);
        return (dto!.SkusUpdated, dto.SkusRemoved);
    }

    public async ValueTask<Dictionary<string, ScryfallResolvedCard>> ResolveEditionsForCardsAsync(
        IEnumerable<string> cardNames, IScryfallApiClient client)
    {
        var resp = await _http.PostAsJsonAsync("/api/cards/resolveEditions", cardNames.ToArray(), JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, ScryfallResolvedCard>>(JsonOpts))!;
    }

    // ── Containers ────────────────────────────────────────────────────────────

    public IEnumerable<ContainerSummaryModel> GetContainers()
    {
        return _http.GetFromJsonAsync<List<ContainerSummaryModel>>("/api/containers", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<ContainerSummaryModel> CreateContainerAsync(string name, string? description)
    {
        var resp = await _http.PostAsJsonAsync("/api/containers", new { name, description }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ContainerSummaryModel>(JsonOpts))!;
    }

    public async ValueTask<ContainerSummaryModel> UpdateContainerAsync(
        int id, string name, string? description)
    {
        var resp = await _http.PutAsJsonAsync($"/api/containers/{id}", new { name, description }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ContainerSummaryModel>(JsonOpts))!;
    }

    public async ValueTask<DeleteContainerResult> DeleteContainerAsync(DeleteContainerInputModel model)
    {
        var resp = await _http.PostAsJsonAsync(
            $"/api/containers/{model.ContainerId}/delete", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DeleteContainerResult>(JsonOpts))!;
    }

    public string PrintContainer(int containerId, ContainerPrintOptions options)
    {
        var qs = BuildQueryString([("reportProxyUsage", options.ReportProxyUsage.ToString().ToLowerInvariant())]);
        return _http.GetStringAsync($"/api/containers/{containerId}/print{qs}")
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    // ── Decks ─────────────────────────────────────────────────────────────────

    public IEnumerable<DeckSummaryModel> GetDecks(DeckFilterModel? filter)
    {
        var qs = new List<(string, string?)>();
        if (filter?.Formats != null)
        {
            var fmtList = filter.Formats.ToList();
            if (fmtList.Count > 0) qs.Add(("formats", string.Join(",", fmtList)));
        }
        if (filter?.Ids != null)
        {
            var idList = filter.Ids.ToList();
            if (idList.Count > 0) qs.Add(("deckIds", string.Join(",", idList)));
        }

        var url = "/api/decks" + BuildQueryString(qs);
        return _http.GetFromJsonAsync<List<DeckSummaryModel>>(url, JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<DeckModel> GetDeckAsync(
        int deckId, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        var result = await _http.GetFromJsonAsync<DeckModel>($"/api/decks/{deckId}", JsonOpts, cancel);
        return result!;
    }

    public async ValueTask<DeckSummaryModel> CreateDeckAsync(
        string name, string? format, int? containerId, bool isCommander = false)
    {
        var resp = await _http.PostAsJsonAsync(
            "/api/decks", new { name, format, containerId, isCommander }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DeckSummaryModel>(JsonOpts))!;
    }

    public async ValueTask<DeckSummaryModel> UpdateDeckAsync(
        int id, string name, string? format, int? containerId, bool isCommander = false)
    {
        var resp = await _http.PutAsJsonAsync(
            $"/api/decks/{id}", new { name, format, containerId, isCommander }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DeckSummaryModel>(JsonOpts))!;
    }

    public async ValueTask<DeckSummaryModel> SetDeckBannerAsync(int deckId, Guid? cardSkuId)
    {
        var resp = await _http.PostAsJsonAsync(
            $"/api/decks/{deckId}/banner", new { cardSkuId }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DeckSummaryModel>(JsonOpts))!;
    }

    public async ValueTask<DeckSummaryModel> SetDeckCommanderAsync(int deckId, Guid? commanderSkuId)
    {
        var resp = await _http.PostAsJsonAsync(
            $"/api/decks/{deckId}/commander", new { commanderSkuId }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DeckSummaryModel>(JsonOpts))!;
    }

    public async ValueTask<DismantleDeckResult> DismantleDeckAsync(DismantleDeckInputModel model)
    {
        var resp = await _http.PostAsJsonAsync(
            $"/api/decks/{model.DeckId}/dismantle", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<DismantleDeckResult>(JsonOpts))!;
    }

    public async ValueTask<CommanderValidationResult> ValidateCommanderDeckAsync(
        int deckId, CancellationToken cancel)
    {
        var resp = await _http.PostAsync($"/api/decks/{deckId}/validate", content: null, cancel);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CommanderValidationResult>(JsonOpts, cancel))!;
    }

    public IEnumerable<string> GetDeckFormats()
    {
        return _http.GetFromJsonAsync<List<string>>("/api/decks/formats", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public bool HasOtherDecksInFormat(string format)
    {
        return _http.GetFromJsonAsync<bool>(
            $"/api/decks/hasOtherFormats/{Uri.EscapeDataString(format)}", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public string PrintDeck(int deckId, DeckPrintOptions options)
    {
        var qs = BuildQueryString([("reportProxyUsage", options.ReportProxyUsage.ToString().ToLowerInvariant())]);
        return _http.GetStringAsync($"/api/decks/{deckId}/print{qs}")
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    // ── Wishlist ──────────────────────────────────────────────────────────────

    public IEnumerable<WishlistItemModel> GetWishlistItems(WishlistItemFilter filter)
    {
        var qs = new List<(string, string?)>();
        if (filter.Tags != null)
        {
            var tagList = filter.Tags.ToList();
            if (tagList.Count > 0) qs.Add(("tags", string.Join(",", tagList)));
        }

        var url = "/api/wishlist" + BuildQueryString(qs);
        return _http.GetFromJsonAsync<List<WishlistItemModel>>(url, JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<ICollection<WishlistItemModel>> AddMultipleToWishlistAsync(
        IEnumerable<AddToWishlistInputModel> items, IScryfallApiClient? scryfallClient)
    {
        var resp = await _http.PostAsJsonAsync("/api/wishlist", items.ToArray(), JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<WishlistItemModel>>(JsonOpts))!;
    }

    public async ValueTask<WishlistItemModel> UpdateWishlistItemAsync(
        UpdateWishlistItemInputModel model, IScryfallApiClient? scryfallApiClient, CancellationToken cancel)
    {
        var resp = await _http.PutAsJsonAsync($"/api/wishlist/{model.Id}", model, JsonOpts, cancel);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WishlistItemModel>(JsonOpts, cancel))!;
    }

    public async ValueTask<WishlistItemModel> DeleteWishlistItemAsync(int id)
    {
        var resp = await _http.PostAsync($"/api/wishlist/{id}/delete", content: null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WishlistItemModel>(JsonOpts))!;
    }

    public async ValueTask<MoveWishlistItemsToCollectionResult> MoveWishlistItemsToCollectionAsync(
        MoveWishlistItemsToCollectionInputModel model)
    {
        var resp = await _http.PostAsJsonAsync("/api/wishlist/moveToCollection", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MoveWishlistItemsToCollectionResult>(JsonOpts))!;
    }

    public WishlistSpendSummaryModel GetWishlistSpend()
    {
        return _http.GetFromJsonAsync<WishlistSpendSummaryModel>("/api/wishlist/spend", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult()!;
    }

    public WishlistBuyingListModel GenerateBuyingList()
    {
        var entries = _http.GetFromJsonAsync<BuyingListVendorEntry[]>("/api/wishlist/buyingList", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];

        var model = new WishlistBuyingListModel();
        foreach (var entry in entries)
            foreach (var item in entry.Items)
                model.Add(entry.Vendor, item);
        return model;
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    public IEnumerable<NotesModel> GetNotes()
    {
        return _http.GetFromJsonAsync<List<NotesModel>>("/api/notes", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<NotesModel> UpdateNotesAsync(int? id, string? title, string notes)
    {
        var resp = await _http.PostAsJsonAsync("/api/notes", new { id, title, notes }, JsonOpts);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<NotesModel>(JsonOpts))!;
    }

    public async ValueTask<bool> DeleteNotesAsync(int id)
    {
        var resp = await _http.DeleteAsync($"/api/notes/{id}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<bool>(JsonOpts));
    }

    // ── Vendors ───────────────────────────────────────────────────────────────

    public IEnumerable<VendorModel> GetVendors()
    {
        return _http.GetFromJsonAsync<List<VendorModel>>("/api/vendors", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<(int created, int deleted)> ApplyVendorsAsync(ApplyVendorsInputModel model)
    {
        var resp = await _http.PostAsJsonAsync("/api/vendors", model, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<VendorApplyResult>(JsonOpts);
        return (dto!.Created, dto.Deleted);
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    public IEnumerable<string> GetTags()
    {
        return _http.GetFromJsonAsync<List<string>>("/api/tags", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult() ?? [];
    }

    public async ValueTask<ApplyTagsResult> ApplyTagsAsync(
        IEnumerable<string> tags, CancellationToken cancel)
    {
        var resp = await _http.PostAsJsonAsync("/api/tags", tags.ToArray(), JsonOpts, cancel);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ApplyTagsResult>(JsonOpts, cancel))!;
    }

    // ── Collection ────────────────────────────────────────────────────────────

    public CollectionSummaryModel GetCollectionSummary()
    {
        return _http.GetFromJsonAsync<CollectionSummaryModel>("/api/collection/summary", JsonOpts)
            .ConfigureAwait(false).GetAwaiter().GetResult()!;
    }

    public async ValueTask<CheckQuantityResult> CheckQuantityShortfallAsync(
        string cardName, int wantQty, bool noProxies, bool sparesOnly)
    {
        var qs = BuildQueryString([
            ("cardName", cardName),
            ("wantQty", wantQty.ToString()),
            ("noProxies", noProxies.ToString().ToLowerInvariant()),
            ("sparesOnly", sparesOnly.ToString().ToLowerInvariant()),
        ]);
        var result = await _http.GetFromJsonAsync<CheckQuantityResult>(
            $"/api/collection/checkQuantity{qs}", JsonOpts);
        return result!;
    }

    // ── Prices ────────────────────────────────────────────────────────────────

    public async ValueTask<(double? price, string? provider)> GetLatestPriceForSkuAsync(
        Guid skuId, string currency, CancellationToken cancel)
    {
        var qs = BuildQueryString([("currency", currency)]);
        var dto = await _http.GetFromJsonAsync<SkuPriceResult>(
            $"/api/prices/sku/{skuId}{qs}", JsonOpts, cancel);
        return (dto?.Price, dto?.Provider);
    }

    public async ValueTask<CardPriceHistoryModel?> GetPriceHistoryForSkuAsync(
        Guid skuId, string currency, CancellationToken cancel)
    {
        var qs = BuildQueryString([("currency", currency)]);
        return await _http.GetFromJsonAsync<CardPriceHistoryModel>(
            $"/api/prices/sku/{skuId}/history{qs}", JsonOpts, cancel);
    }

    public async ValueTask<List<LowestPriceCheckItem>> GetLowestPricesAsync(
        LowestPriceCheckOptions options, IScryfallApiClient client, CancellationToken cancel)
    {
        var resp = await _http.PostAsJsonAsync("/api/prices/lowest", options, JsonOpts, cancel);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<LowestPriceCheckItem>>(JsonOpts, cancel))!;
    }

    // ── Card images ───────────────────────────────────────────────────────────

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/front/large",
                HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode
                ? await resp.Content.ReadAsStreamAsync()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/front/small",
                HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode
                ? await resp.Content.ReadAsStreamAsync()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/back/large",
                HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode
                ? await resp.Content.ReadAsStreamAsync()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/images/{scryfallId}/back/small",
                HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode
                ? await resp.Content.ReadAsStreamAsync()
                : null;
        }
        catch
        {
            return null;
        }
    }

    // ── Scryfall identifiers ──────────────────────────────────────────────────

    public async ValueTask<bool> IsScryfallIdMappingEmptyAsync(CancellationToken cancel)
    {
        return await _http.GetFromJsonAsync<bool>("/api/identifiers/isEmpty", JsonOpts, cancel);
    }

    public async ValueTask ImportCardIdentifiersAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        await StreamSseAsync("/api/identifiers/import", null, callback, cancel);
    }

    // ── Metadata operations (long-running, SSE) ───────────────────────────────

    public async ValueTask<IEnumerable<CardSkuModel>> UpdateCardMetadataAsync(
        ICollection<Guid> ids,
        IScryfallApiClient scryfallApiClient,
        UpdateCardMetadataProgressCallback? callback,
        CancellationToken cancel)
    {
        var payload = await StreamSseAsync(
            "/api/metadata/updateCards", new { ids }, callback, cancel);

        if (string.IsNullOrEmpty(payload))
            return [];

        return JsonSerializer.Deserialize<List<CardSkuModel>>(payload, JsonOpts) ?? [];
    }

    public async ValueTask<IEnumerable<WishlistItemModel>> UpdateWishlistMetadataAsync(
        ICollection<int> ids,
        IScryfallApiClient scryfallApiClient,
        UpdateCardMetadataProgressCallback? callback,
        CancellationToken cancel)
    {
        var payload = await StreamSseAsync(
            "/api/metadata/updateWishlist", new { ids }, callback, cancel);

        if (string.IsNullOrEmpty(payload))
            return [];

        return JsonSerializer.Deserialize<List<WishlistItemModel>>(payload, JsonOpts) ?? [];
    }

    public async ValueTask AddMissingMetadataAsync(
        UpdateCardMetadataProgressCallback callback,
        IScryfallApiClient scryfallApiClient,
        CancellationToken cancel)
    {
        await StreamSseAsync("/api/metadata/addMissing", null, callback, cancel);
    }

    public async ValueTask RebuildAllMetadataAsync(
        UpdateCardMetadataProgressCallback callback,
        IScryfallApiClient scryfallApiClient,
        CancellationToken cancel)
    {
        await StreamSseAsync("/api/metadata/rebuildAll", null, callback, cancel);
    }

    public async ValueTask NormalizeCardNamesAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        await StreamSseAsync("/api/metadata/normalizeNames", null, callback, cancel);
    }

    public async ValueTask<bool> ImportPriceDataAsync(
        UpdateCardMetadataProgressCallback callback, CancellationToken cancel)
    {
        var payload = await StreamSseAsync("/api/prices/import", null, callback, cancel);
        return !string.IsNullOrEmpty(payload)
            && JsonSerializer.Deserialize<bool>(payload, JsonOpts);
    }
}
