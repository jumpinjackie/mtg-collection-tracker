using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using StrongInject;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

// ── Configuration ─────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

var serverSection = builder.Configuration.GetSection("Server");
var port = serverSection.GetValue<int>("Port", 5757);
var apiKey = serverSection.GetValue<string>("ApiKey");
var dbPath = serverSection.GetValue<string>("DbPath") ?? "collection.sqlite";

builder.WebHost.ConfigureKestrel(opts => opts.ListenAnyIP(port));

// ── Services ──────────────────────────────────────────────────────────────────

var services = builder.Services;

// DbContext – transient so every Owned<CardsDbContext> wrapper is a fresh unit-of-work.
services.AddDbContext<CardsDbContext>(
    opts => opts.UseSqlite($"Data Source={dbPath}"),
    ServiceLifetime.Transient);

// Factory used by CollectionTrackingService and CardImageCache.
services.AddTransient<Func<Owned<CardsDbContext>>>(sp => () =>
{
    var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
    return new Owned<CardsDbContext>(db, () => scope.Dispose());
});

// Card-image filesystem – singleton, stored alongside the database.
var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? Directory.GetCurrentDirectory();
var imageDir = Path.Combine(dbDir, "card-images");
Directory.CreateDirectory(imageDir);
services.AddSingleton<ICardImageFileSystem>(new CardImageFileSystem(imageDir));

// Scryfall HTTP client – singleton.
var scryfallHttp = new HttpClient(new HttpClientHandler())
{
    BaseAddress = new Uri("https://api.scryfall.com/")
};
scryfallHttp.DefaultRequestHeaders.Accept.Add(
    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
scryfallHttp.DefaultRequestHeaders.Add(
    "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
var scryfallClient = new ScryfallClient(scryfallHttp);
services.AddSingleton<IScryfallApiClient>(scryfallClient);

services.AddScoped<CardImageCache>();
services.AddSingleton<PriceCache>();
services.AddScoped<ICollectionTrackingService, CollectionTrackingService>();
services.AddSingleton<OperationManager>();

var app = builder.Build();

// ── Database migration ────────────────────────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
    await db.Database.MigrateAsync();
}

// ── API-key middleware ────────────────────────────────────────────────────────

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/health"))
    {
        await next(context);
        return;
    }

    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        var keyFromHeader = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var keyFromQuery = context.Request.Query["api_key"].FirstOrDefault();
        var provided = keyFromHeader ?? keyFromQuery;

        if (provided != apiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }
    }

    await next(context);
});

// ── Endpoints ─────────────────────────────────────────────────────────────────

// Health
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = "1.0.0" }));

// Languages
app.MapGet("/api/languages", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetLanguages()));

// Tags
app.MapGet("/api/tags", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetTags()));

app.MapPost("/api/tags", async (
    string[] tags,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var result = await svc.ApplyTagsAsync(tags, cancel);
    return Results.Ok(result);
});

// ── Cards ─────────────────────────────────────────────────────────────────────

app.MapGet("/api/cards", (
    string? searchFilter,
    bool notInDecks,
    bool noProxies,
    bool unParented,
    bool missingMetadata,
    bool includeScryfallMetadata,
    string? deckIds,
    string? containerIds,
    string? tags,
    string? colors,
    string? cardTypes,
    ICollectionTrackingService svc) =>
{
    var query = new CardQueryModel
    {
        SearchFilter = searchFilter,
        NotInDecks = notInDecks,
        NoProxies = noProxies,
        UnParented = unParented,
        MissingMetadata = missingMetadata,
        IncludeScryfallMetadata = includeScryfallMetadata,
        DeckIds = string.IsNullOrWhiteSpace(deckIds) ? null
            : deckIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray(),
        ContainerIds = string.IsNullOrWhiteSpace(containerIds) ? null
            : containerIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray(),
        Tags = string.IsNullOrWhiteSpace(tags) ? null
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries),
        Colors = string.IsNullOrWhiteSpace(colors) ? null
            : colors.Split(',', StringSplitOptions.RemoveEmptyEntries),
        CardTypes = string.IsNullOrWhiteSpace(cardTypes) ? null
            : cardTypes.Split(',', StringSplitOptions.RemoveEmptyEntries),
    };
    return Results.Ok(svc.GetCards(query));
});

app.MapGet("/api/cards/isBasicLand/{cardName}", (string cardName, ICollectionTrackingService svc) =>
    Results.Ok(svc.IsBasicLand(cardName)));

app.MapGet("/api/cards/container/{containerId:int}", (
    int containerId,
    int page,
    int? pageSize,
    bool missingMetadata,
    ICollectionTrackingService svc) =>
{
    var opts = new FetchContainerPageModel
    {
        PageNumber = page,
        PageSize = pageSize,
        ShowOnlyMissingMetadata = missingMetadata,
    };
    return Results.Ok(svc.GetCardsForContainer(containerId, opts));
});

app.MapGet("/api/cards/{id:guid}", async (
    Guid id,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var card = await svc.GetCardSkuByIdAsync(id, cancel);
    return Results.Ok(card);
});

app.MapPost("/api/cards/add", async (
    int? containerId,
    int? deckId,
    AddToDeckOrContainerInputModel model,
    ICollectionTrackingService svc) =>
{
    var effectiveDeckId = deckId ?? model.DeckId;
    var sku = await svc.AddToDeckOrContainerAsync(containerId, effectiveDeckId, model);
    return Results.Ok(sku);
});

app.MapPost("/api/cards/addToDeck", async (
    AddToDeckInputModel model,
    ICollectionTrackingService svc) =>
{
    var sku = await svc.AddToDeckAsync(model);
    return Results.Ok(sku);
});

app.MapPost("/api/cards/addBatch", async (
    AddBatchRequest req,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi) =>
{
    var (total, proxyTotal, rows) = await svc.AddMultipleToContainerOrDeckAsync(
        req.ContainerId, req.DeckId, req.Items, scryfallApi);
    return Results.Ok(new { total, proxyTotal, rows });
});

app.MapPut("/api/cards/{id:guid}", async (
    Guid id,
    UpdateCardSkuInputModel model,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi,
    CancellationToken cancel) =>
{
    model.Ids = new[] { id };
    var result = await svc.UpdateCardSkuAsync(model, scryfallApi, cancel);
    return Results.Ok(result);
});

app.MapPost("/api/cards/{id:guid}/split", async (
    Guid id,
    SplitCardSkuInputModel model,
    ICollectionTrackingService svc) =>
{
    model.CardSkuId = id;
    var sku = await svc.SplitCardSkuAsync(model);
    return Results.Ok(sku);
});

app.MapPost("/api/cards/{id:guid}/delete", async (
    Guid id,
    ICollectionTrackingService svc) =>
{
    var sku = await svc.DeleteCardSkuAsync(id);
    return Results.Ok(sku);
});

app.MapPost("/api/cards/removeFromDeck", async (
    RemoveFromDeckInputModel model,
    ICollectionTrackingService svc) =>
{
    var (sku, wasMerged) = await svc.RemoveFromDeckAsync(model);
    return Results.Ok(new { sku, wasMerged });
});

app.MapPost("/api/cards/consolidate", async (
    ConsolidateCardSkusInputModel model,
    ICollectionTrackingService svc) =>
{
    var (skusUpdated, skusRemoved) = await svc.ConsolidateCardSkusAsync(model);
    return Results.Ok(new { skusUpdated, skusRemoved });
});

app.MapPost("/api/cards/resolveEditions", async (
    string[] cardNames,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi) =>
{
    var result = await svc.ResolveEditionsForCardsAsync(cardNames, scryfallApi);
    return Results.Ok(result);
});

// ── Containers ────────────────────────────────────────────────────────────────

app.MapGet("/api/containers", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetContainers()));

app.MapPost("/api/containers", async (
    CreateContainerRequest req,
    ICollectionTrackingService svc) =>
{
    var container = await svc.CreateContainerAsync(req.Name, req.Description);
    return Results.Ok(container);
});

app.MapPut("/api/containers/{id:int}", async (
    int id,
    CreateContainerRequest req,
    ICollectionTrackingService svc) =>
{
    var container = await svc.UpdateContainerAsync(id, req.Name, req.Description);
    return Results.Ok(container);
});

app.MapPost("/api/containers/{id:int}/delete", async (
    int id,
    DeleteContainerInputModel model,
    ICollectionTrackingService svc) =>
{
    model.ContainerId = id;
    var result = await svc.DeleteContainerAsync(model);
    return Results.Ok(result);
});

app.MapGet("/api/containers/{id:int}/print", (
    int id,
    bool reportProxyUsage,
    ICollectionTrackingService svc) =>
{
    var text = svc.PrintContainer(id, new ContainerPrintOptions(reportProxyUsage));
    return Results.Text(text, "text/plain");
});

// ── Decks ─────────────────────────────────────────────────────────────────────

app.MapGet("/api/decks", (
    string? formats,
    string? deckIds,
    bool? isCommander,
    ICollectionTrackingService svc) =>
{
    DeckFilterModel? filter = null;
    if (!string.IsNullOrWhiteSpace(formats) || !string.IsNullOrWhiteSpace(deckIds))
    {
        filter = new DeckFilterModel
        {
            Formats = string.IsNullOrWhiteSpace(formats)
                ? []
                : formats.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Ids = string.IsNullOrWhiteSpace(deckIds) ? null
                : deckIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse),
        };
    }

    var decks = svc.GetDecks(filter);
    if (isCommander.HasValue)
        decks = decks.Where(d => d.IsCommander == isCommander.Value);
    return Results.Ok(decks);
});

app.MapGet("/api/decks/formats", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetDeckFormats()));

app.MapGet("/api/decks/hasOtherFormats/{format}", (string format, ICollectionTrackingService svc) =>
    Results.Ok(svc.HasOtherDecksInFormat(format)));

app.MapGet("/api/decks/{id:int}", async (
    int id,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi,
    CancellationToken cancel) =>
{
    var deck = await svc.GetDeckAsync(id, scryfallApi, cancel);
    return Results.Ok(deck);
});

app.MapGet("/api/decks/{id:int}/print", (
    int id,
    bool reportProxyUsage,
    ICollectionTrackingService svc) =>
{
    var text = svc.PrintDeck(id, new DeckPrintOptions(reportProxyUsage));
    return Results.Text(text, "text/plain");
});

app.MapPost("/api/decks", async (
    CreateDeckRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.CreateDeckAsync(req.Name, req.Format, req.ContainerId, req.IsCommander);
    return Results.Ok(deck);
});

app.MapPut("/api/decks/{id:int}", async (
    int id,
    CreateDeckRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.UpdateDeckAsync(id, req.Name, req.Format, req.ContainerId, req.IsCommander);
    return Results.Ok(deck);
});

app.MapPost("/api/decks/{id:int}/banner", async (
    int id,
    SetBannerRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.SetDeckBannerAsync(id, req.CardSkuId);
    return Results.Ok(deck);
});

app.MapPost("/api/decks/{id:int}/commander", async (
    int id,
    SetCommanderRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.SetDeckCommanderAsync(id, req.CommanderSkuId);
    return Results.Ok(deck);
});

app.MapPost("/api/decks/{id:int}/dismantle", async (
    int id,
    DismantleDeckInputModel model,
    ICollectionTrackingService svc) =>
{
    model.DeckId = id;
    var result = await svc.DismantleDeckAsync(model);
    return Results.Ok(result);
});

app.MapPost("/api/decks/{id:int}/validate", async (
    int id,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var result = await svc.ValidateCommanderDeckAsync(id, cancel);
    return Results.Ok(result);
});

// ── Wishlist ──────────────────────────────────────────────────────────────────

app.MapGet("/api/wishlist", (
    string? tags,
    ICollectionTrackingService svc) =>
{
    var filter = new WishlistItemFilter(
        string.IsNullOrWhiteSpace(tags) ? null
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries));
    return Results.Ok(svc.GetWishlistItems(filter));
});

app.MapGet("/api/wishlist/spend", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetWishlistSpend()));

app.MapGet("/api/wishlist/buyingList", (ICollectionTrackingService svc) =>
{
    var model = svc.GenerateBuyingList();
    var entries = model.Entries
        .Select(kv => new BuyingListVendorEntry(kv.Key, kv.Value.ToArray()))
        .ToArray();
    return Results.Ok(entries);
});

app.MapPost("/api/wishlist", async (
    IEnumerable<AddToWishlistInputModel> items,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi) =>
{
    var result = await svc.AddMultipleToWishlistAsync(items, scryfallApi);
    return Results.Ok(result);
});

app.MapPut("/api/wishlist/{id:int}", async (
    int id,
    UpdateWishlistItemInputModel model,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi,
    CancellationToken cancel) =>
{
    model.Id = id;
    var item = await svc.UpdateWishlistItemAsync(model, scryfallApi, cancel);
    return Results.Ok(item);
});

app.MapPost("/api/wishlist/{id:int}/delete", async (
    int id,
    ICollectionTrackingService svc) =>
{
    var item = await svc.DeleteWishlistItemAsync(id);
    return Results.Ok(item);
});

app.MapPost("/api/wishlist/moveToCollection", async (
    MoveWishlistItemsToCollectionInputModel model,
    ICollectionTrackingService svc) =>
{
    var result = await svc.MoveWishlistItemsToCollectionAsync(model);
    return Results.Ok(result);
});

// ── Notes ─────────────────────────────────────────────────────────────────────

app.MapGet("/api/notes", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetNotes()));

app.MapPost("/api/notes", async (
    UpdateNotesRequest req,
    ICollectionTrackingService svc) =>
{
    var note = await svc.UpdateNotesAsync(req.Id, req.Title, req.Notes);
    return Results.Ok(note);
});

app.MapDelete("/api/notes/{id:int}", async (
    int id,
    ICollectionTrackingService svc) =>
{
    var result = await svc.DeleteNotesAsync(id);
    return Results.Ok(result);
});

// ── Vendors ───────────────────────────────────────────────────────────────────

app.MapGet("/api/vendors", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetVendors()));

app.MapPost("/api/vendors", async (
    ApplyVendorsInputModel model,
    ICollectionTrackingService svc) =>
{
    var (created, deleted) = await svc.ApplyVendorsAsync(model);
    return Results.Ok(new { created, deleted });
});

// ── Collection ────────────────────────────────────────────────────────────────

app.MapGet("/api/collection/summary", (ICollectionTrackingService svc) =>
    Results.Ok(svc.GetCollectionSummary()));

app.MapGet("/api/collection/checkQuantity", async (
    string cardName,
    int wantQty,
    bool noProxies,
    bool sparesOnly,
    ICollectionTrackingService svc) =>
{
    var result = await svc.CheckQuantityShortfallAsync(cardName, wantQty, noProxies, sparesOnly);
    return Results.Ok(result);
});

// ── Prices ────────────────────────────────────────────────────────────────────

app.MapGet("/api/prices/sku/{skuId:guid}", async (
    Guid skuId,
    string? currency,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var (price, provider) = await svc.GetLatestPriceForSkuAsync(skuId, currency ?? "USD", cancel);
    return Results.Ok(new { price, provider });
});

app.MapGet("/api/prices/sku/{skuId:guid}/history", async (
    Guid skuId,
    string? currency,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var history = await svc.GetPriceHistoryForSkuAsync(skuId, currency ?? "USD", cancel);
    return Results.Ok(history);
});

app.MapPost("/api/prices/lowest", async (
    LowestPriceCheckOptions opts,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi,
    CancellationToken cancel) =>
{
    var result = await svc.GetLowestPricesAsync(opts, scryfallApi, cancel);
    return Results.Ok(result);
});

// ── Card images ───────────────────────────────────────────────────────────────

app.MapGet("/api/images/{scryfallId}/front/large", async (
    string scryfallId,
    ICollectionTrackingService svc) =>
{
    if (!IsValidScryfallId(scryfallId)) return Results.BadRequest("Invalid Scryfall ID");
    var stream = await svc.GetLargeFrontFaceImageAsync(scryfallId);
    return stream is null ? Results.NotFound() : Results.Stream(stream, "image/jpeg");
});

app.MapGet("/api/images/{scryfallId}/front/small", async (
    string scryfallId,
    ICollectionTrackingService svc) =>
{
    if (!IsValidScryfallId(scryfallId)) return Results.BadRequest("Invalid Scryfall ID");
    var stream = await svc.GetSmallFrontFaceImageAsync(scryfallId);
    return stream is null ? Results.NotFound() : Results.Stream(stream, "image/jpeg");
});

app.MapGet("/api/images/{scryfallId}/back/large", async (
    string scryfallId,
    ICollectionTrackingService svc) =>
{
    if (!IsValidScryfallId(scryfallId)) return Results.BadRequest("Invalid Scryfall ID");
    var stream = await svc.GetLargeBackFaceImageAsync(scryfallId);
    return stream is null ? Results.NotFound() : Results.Stream(stream, "image/jpeg");
});

app.MapGet("/api/images/{scryfallId}/back/small", async (
    string scryfallId,
    ICollectionTrackingService svc) =>
{
    if (!IsValidScryfallId(scryfallId)) return Results.BadRequest("Invalid Scryfall ID");
    var stream = await svc.GetSmallBackFaceImageAsync(scryfallId);
    return stream is null ? Results.NotFound() : Results.Stream(stream, "image/jpeg");
});

// ── Scryfall identifiers ──────────────────────────────────────────────────────

app.MapGet("/api/identifiers/isEmpty", async (
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var isEmpty = await svc.IsScryfallIdMappingEmptyAsync(cancel);
    return Results.Ok(isEmpty);
});

// ── Long-running operations with SSE ─────────────────────────────────────────

app.MapPost("/api/metadata/updateCards", (
    UpdateCardsMetadataRequest req,
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    IScryfallApiClient scryfallApi,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            var result = await svc.UpdateCardMetadataAsync(req.Ids, scryfallApi, callback, CancellationToken.None);
            var payload = JsonSerializer.Serialize(result);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done", Payload = payload });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateCardMetadataAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

app.MapPost("/api/metadata/updateWishlist", (
    UpdateWishlistMetadataRequest req,
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    IScryfallApiClient scryfallApi,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            var result = await svc.UpdateWishlistMetadataAsync(req.Ids, scryfallApi, callback, CancellationToken.None);
            var payload = JsonSerializer.Serialize(result);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done", Payload = payload });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateWishlistMetadataAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

app.MapPost("/api/metadata/addMissing", (
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    IScryfallApiClient scryfallApi,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            await svc.AddMissingMetadataAsync(callback, scryfallApi, CancellationToken.None);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in AddMissingMetadataAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

app.MapPost("/api/metadata/rebuildAll", (
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    IScryfallApiClient scryfallApi,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            await svc.RebuildAllMetadataAsync(callback, scryfallApi, CancellationToken.None);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RebuildAllMetadataAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

app.MapPost("/api/metadata/normalizeNames", (
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            await svc.NormalizeCardNamesAsync(callback, CancellationToken.None);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in NormalizeCardNamesAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

app.MapPost("/api/identifiers/import", (
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            await svc.ImportCardIdentifiersAsync(callback, CancellationToken.None);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ImportCardIdentifiersAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

app.MapPost("/api/prices/import", (
    OperationManager opMgr,
    IServiceScopeFactory scopeFactory,
    ILogger<Program> logger) =>
{
    var opId = opMgr.CreateOperation();
    _ = Task.Run(async () =>
    {
        var ch = opMgr.GetChannel(opId)!;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICollectionTrackingService>();
            var callback = opMgr.CreateCallback(opId);
            var imported = await svc.ImportPriceDataAsync(callback, CancellationToken.None);
            var payload = JsonSerializer.Serialize(imported);
            ch.Writer.TryWrite(new ProgressEvent { Type = "done", Payload = payload });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ImportPriceDataAsync");
            ch.Writer.TryWrite(new ProgressEvent { Type = "error", Message = ex.Message });
        }
        finally
        {
            ch.Writer.TryComplete();
        }
    });
    return Results.Accepted($"/api/operations/{opId}/events", new { operationId = opId });
});

// SSE event stream
app.MapGet("/api/operations/{operationId}/events", async (
    string operationId,
    OperationManager opMgr,
    HttpContext context,
    CancellationToken cancel) =>
{
    var ch = opMgr.GetChannel(operationId);
    if (ch is null)
    {
        context.Response.StatusCode = 404;
        return;
    }

    context.Response.Headers["Content-Type"] = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["X-Accel-Buffering"] = "no";

    try
    {
        await foreach (var evt in ch.Reader.ReadAllAsync(cancel))
        {
            var json = JsonSerializer.Serialize(evt);
            await context.Response.WriteAsync($"data: {json}\n\n", cancel);
            await context.Response.Body.FlushAsync(cancel);
        }
    }
    catch (OperationCanceledException)
    {
        // Client disconnected – clean up below.
    }
    finally
    {
        opMgr.RemoveOperation(operationId);
    }
});

app.Run();

// ── Local helper ──────────────────────────────────────────────────────────────

/// <summary>Validates that a Scryfall ID contains only alphanumeric characters and hyphens.</summary>
static bool IsValidScryfallId(string scryfallId) =>
    !string.IsNullOrEmpty(scryfallId) &&
    scryfallId.Length <= 64 &&
    scryfallId.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

// ── Helper types ──────────────────────────────────────────────────────────────

record CreateContainerRequest(string Name, string? Description);
record CreateDeckRequest(string Name, string? Format, int? ContainerId, bool IsCommander);
record SetBannerRequest(Guid? CardSkuId);
record SetCommanderRequest(Guid? CommanderSkuId);
record AddBatchRequest(int? ContainerId, int? DeckId, IEnumerable<AddToDeckOrContainerInputModel> Items);
record UpdateCardsMetadataRequest(ICollection<Guid> Ids);
record UpdateWishlistMetadataRequest(ICollection<int> Ids);
record UpdateNotesRequest(int? Id, string? Title, string Notes);
record BuyingListVendorEntry(string Vendor, BuyingListItem[] Items);

/// <summary>Represents a single SSE progress event sent to the client.</summary>
public class ProgressEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "progress";

    [JsonPropertyName("current")]
    public int Current { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>JSON-encoded result payload, present on "done" events that return data.</summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>Manages long-running background operations and their SSE channels.</summary>
public class OperationManager
{
    private readonly ConcurrentDictionary<string, Channel<ProgressEvent>> _channels = new();

    public string CreateOperation()
    {
        var id = Guid.NewGuid().ToString("N");
        _channels[id] = Channel.CreateUnbounded<ProgressEvent>();
        return id;
    }

    public Channel<ProgressEvent>? GetChannel(string operationId)
    {
        _channels.TryGetValue(operationId, out var ch);
        return ch;
    }

    public void RemoveOperation(string operationId) =>
        _channels.TryRemove(operationId, out _);

    public UpdateCardMetadataProgressCallback CreateCallback(string operationId)
    {
        var ch = _channels[operationId];
        return new UpdateCardMetadataProgressCallback
        {
            OnProgress = (current, total) =>
                ch.Writer.TryWrite(new ProgressEvent { Type = "progress", Current = current, Total = total }),
            OnDownloadStatus = msg =>
                ch.Writer.TryWrite(new ProgressEvent { Type = "download", Message = msg }),
        };
    }
}
