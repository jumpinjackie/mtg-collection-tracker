using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using Scalar.AspNetCore;
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

services.AddOpenApi();
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

// OpenAPI document endpoint (design-time spec generation uses this too)
app.MapOpenApi();

// Scalar API explorer UI – available at /scalar
app.MapScalarApiReference(options =>
{
    options.WithTitle("MtgCollectionTracker API")
           .WithOpenApiRoutePattern("/openapi/{documentName}.json");
});

// ── API-key middleware ────────────────────────────────────────────────────────

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/health") ||
        context.Request.Path.StartsWithSegments("/openapi") ||
        context.Request.Path.StartsWithSegments("/scalar"))
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
app.MapGet("/api/health", () => TypedResults.Ok(new { status = "ok", version = "1.0.0" }));

// Languages
app.MapGet("/api/languages", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetLanguages()));

// Tags
app.MapGet("/api/tags", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetTags()));

app.MapPost("/api/tags", async (
    string[] tags,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var result = await svc.ApplyTagsAsync(tags, cancel);
    return TypedResults.Ok(result);
});

// ── Cards ─────────────────────────────────────────────────────────────────────

app.MapGet("/api/cards", (
    ICollectionTrackingService svc,
    string? searchFilter,
    bool notInDecks = false,
    bool noProxies = false,
    bool unParented = false,
    bool missingMetadata = false,
    bool includeScryfallMetadata = false,
    [Microsoft.AspNetCore.Mvc.FromQuery] int[]? deckIds = null,
    [Microsoft.AspNetCore.Mvc.FromQuery] int[]? containerIds = null,
    [Microsoft.AspNetCore.Mvc.FromQuery] string[]? tags = null,
    [Microsoft.AspNetCore.Mvc.FromQuery] string[]? colors = null,
    [Microsoft.AspNetCore.Mvc.FromQuery] string[]? cardTypes = null) =>
{
    var query = new CardQueryModel
    {
        SearchFilter = searchFilter,
        NotInDecks = notInDecks,
        NoProxies = noProxies,
        UnParented = unParented,
        MissingMetadata = missingMetadata,
        IncludeScryfallMetadata = includeScryfallMetadata,
        DeckIds = deckIds,
        ContainerIds = containerIds,
        Tags = tags,
        Colors = colors,
        CardTypes = cardTypes,
    };
    return TypedResults.Ok(svc.GetCards(query));
});

app.MapGet("/api/cards/isBasicLand/{cardName}", (string cardName, ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.IsBasicLand(cardName)));

app.MapGet("/api/cards/container/{containerId:int}", (
    ICollectionTrackingService svc,
    int containerId,
    int page,
    int? pageSize,
    bool missingMetadata = false) =>
{
    var opts = new FetchContainerPageModel
    {
        PageNumber = page,
        PageSize = pageSize,
        ShowOnlyMissingMetadata = missingMetadata,
    };
    return TypedResults.Ok(svc.GetCardsForContainer(containerId, opts));
});

app.MapGet("/api/cards/{id:guid}", async (
    Guid id,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var card = await svc.GetCardSkuByIdAsync(id, cancel);
    return TypedResults.Ok(card);
});

app.MapPost("/api/cards/add", async (
    int? containerId,
    int? deckId,
    AddToDeckOrContainerInputModel model,
    ICollectionTrackingService svc) =>
{
    var effectiveDeckId = deckId ?? model.DeckId;
    var sku = await svc.AddToDeckOrContainerAsync(containerId, effectiveDeckId, model);
    return TypedResults.Ok(sku);
});

app.MapPost("/api/cards/addToDeck", async (
    AddToDeckInputModel model,
    ICollectionTrackingService svc) =>
{
    var sku = await svc.AddToDeckAsync(model);
    return TypedResults.Ok(sku);
});

app.MapPost("/api/cards/addBatch", async (
    AddBatchRequest req,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi) =>
{
    var (total, proxyTotal, rows) = await svc.AddMultipleToContainerOrDeckAsync(
        req.ContainerId, req.DeckId, req.Items, scryfallApi);
    return TypedResults.Ok(new AddBatchResult(total, proxyTotal, rows));
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
    return TypedResults.Ok(result);
});

app.MapPost("/api/cards/{id:guid}/split", async (
    Guid id,
    SplitCardSkuInputModel model,
    ICollectionTrackingService svc) =>
{
    model.CardSkuId = id;
    var sku = await svc.SplitCardSkuAsync(model);
    return TypedResults.Ok(sku);
});

app.MapPost("/api/cards/{id:guid}/delete", async (
    Guid id,
    ICollectionTrackingService svc) =>
{
    var sku = await svc.DeleteCardSkuAsync(id);
    return TypedResults.Ok(sku);
});

app.MapPost("/api/cards/removeFromDeck", async (
    RemoveFromDeckInputModel model,
    ICollectionTrackingService svc) =>
{
    var (sku, wasMerged) = await svc.RemoveFromDeckAsync(model);
    return TypedResults.Ok(new RemoveFromDeckResult(sku, wasMerged));
});

app.MapPost("/api/cards/consolidate", async (
    ConsolidateCardSkusInputModel model,
    ICollectionTrackingService svc) =>
{
    var (skusUpdated, skusRemoved) = await svc.ConsolidateCardSkusAsync(model);
    return TypedResults.Ok(new ConsolidateResult(skusUpdated, skusRemoved));
});

app.MapPost("/api/cards/resolveEditions", async (
    string[] cardNames,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi) =>
{
    var result = await svc.ResolveEditionsForCardsAsync(cardNames, scryfallApi);
    return TypedResults.Ok(result);
});

// ── Containers ────────────────────────────────────────────────────────────────

app.MapGet("/api/containers", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetContainers()));

app.MapPost("/api/containers", async (
    CreateContainerRequest req,
    ICollectionTrackingService svc) =>
{
    var container = await svc.CreateContainerAsync(req.Name, req.Description);
    return TypedResults.Ok(container);
});

app.MapPut("/api/containers/{id:int}", async (
    int id,
    CreateContainerRequest req,
    ICollectionTrackingService svc) =>
{
    var container = await svc.UpdateContainerAsync(id, req.Name, req.Description);
    return TypedResults.Ok(container);
});

app.MapPost("/api/containers/{id:int}/delete", async (
    int id,
    DeleteContainerInputModel model,
    ICollectionTrackingService svc) =>
{
    model.ContainerId = id;
    var result = await svc.DeleteContainerAsync(model);
    return TypedResults.Ok(result);
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
    [Microsoft.AspNetCore.Mvc.FromQuery] string[]? formats,
    [Microsoft.AspNetCore.Mvc.FromQuery] int[]? deckIds,
    bool? isCommander,
    ICollectionTrackingService svc) =>
{
    DeckFilterModel? filter = null;
    if (formats?.Length > 0 || deckIds?.Length > 0)
    {
        filter = new DeckFilterModel
        {
            Formats = formats ?? [],
            Ids = deckIds,
        };
    }

    var decks = svc.GetDecks(filter);
    if (isCommander.HasValue)
        decks = decks.Where(d => d.IsCommander == isCommander.Value);
    return TypedResults.Ok(decks);
});

app.MapGet("/api/decks/formats", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetDeckFormats()));

app.MapGet("/api/decks/hasOtherFormats/{format}", (string format, ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.HasOtherDecksInFormat(format)));

app.MapGet("/api/decks/{id:int}", async (
    int id,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi,
    CancellationToken cancel) =>
{
    var deck = await svc.GetDeckAsync(id, scryfallApi, cancel);
    return TypedResults.Ok(deck);
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
    return TypedResults.Ok(deck);
});

app.MapPut("/api/decks/{id:int}", async (
    int id,
    CreateDeckRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.UpdateDeckAsync(id, req.Name, req.Format, req.ContainerId, req.IsCommander);
    return TypedResults.Ok(deck);
});

app.MapPost("/api/decks/{id:int}/banner", async (
    int id,
    SetBannerRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.SetDeckBannerAsync(id, req.CardSkuId);
    return TypedResults.Ok(deck);
});

app.MapPost("/api/decks/{id:int}/commander", async (
    int id,
    SetCommanderRequest req,
    ICollectionTrackingService svc) =>
{
    var deck = await svc.SetDeckCommanderAsync(id, req.CommanderSkuId);
    return TypedResults.Ok(deck);
});

app.MapPost("/api/decks/{id:int}/dismantle", async (
    int id,
    DismantleDeckInputModel model,
    ICollectionTrackingService svc) =>
{
    model.DeckId = id;
    var result = await svc.DismantleDeckAsync(model);
    return TypedResults.Ok(result);
});

app.MapPost("/api/decks/{id:int}/validate", async (
    int id,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var result = await svc.ValidateCommanderDeckAsync(id, cancel);
    return TypedResults.Ok(result);
});

// ── Wishlist ──────────────────────────────────────────────────────────────────

app.MapGet("/api/wishlist", (
    [Microsoft.AspNetCore.Mvc.FromQuery] string[]? tags,
    ICollectionTrackingService svc) =>
{
    var filter = new WishlistItemFilter(tags?.Length > 0 ? tags : null);
    return TypedResults.Ok(svc.GetWishlistItems(filter));
});

app.MapGet("/api/wishlist/spend", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetWishlistSpend()));

app.MapGet("/api/wishlist/buyingList", (ICollectionTrackingService svc) =>
{
    var model = svc.GenerateBuyingList();
    var entries = model.Entries
        .Select(kv => new BuyingListVendorEntry(kv.Key, kv.Value.ToArray()))
        .ToArray();
    return TypedResults.Ok(entries);
});

app.MapPost("/api/wishlist", async (
    [Microsoft.AspNetCore.Mvc.FromBody] IEnumerable<AddToWishlistInputModel> items,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi) =>
{
    var result = await svc.AddMultipleToWishlistAsync(items, scryfallApi);
    return TypedResults.Ok(result);
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
    return TypedResults.Ok(item);
});

app.MapPost("/api/wishlist/{id:int}/delete", async (
    int id,
    ICollectionTrackingService svc) =>
{
    var item = await svc.DeleteWishlistItemAsync(id);
    return TypedResults.Ok(item);
});

app.MapPost("/api/wishlist/moveToCollection", async (
    MoveWishlistItemsToCollectionInputModel model,
    ICollectionTrackingService svc) =>
{
    var result = await svc.MoveWishlistItemsToCollectionAsync(model);
    return TypedResults.Ok(result);
});

// ── Notes ─────────────────────────────────────────────────────────────────────

app.MapGet("/api/notes", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetNotes()));

app.MapPost("/api/notes", async (
    UpdateNotesRequest req,
    ICollectionTrackingService svc) =>
{
    var note = await svc.UpdateNotesAsync(req.Id, req.Title, req.Notes);
    return TypedResults.Ok(note);
});

app.MapDelete("/api/notes/{id:int}", async (
    int id,
    ICollectionTrackingService svc) =>
{
    var result = await svc.DeleteNotesAsync(id);
    return TypedResults.Ok(result);
});

// ── Vendors ───────────────────────────────────────────────────────────────────

app.MapGet("/api/vendors", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetVendors()));

app.MapPost("/api/vendors", async (
    ApplyVendorsInputModel model,
    ICollectionTrackingService svc) =>
{
    var (created, deleted) = await svc.ApplyVendorsAsync(model);
    return TypedResults.Ok(new ApplyVendorsResult(created, deleted));
});

// ── Collection ────────────────────────────────────────────────────────────────

app.MapGet("/api/collection/summary", (ICollectionTrackingService svc) =>
    TypedResults.Ok(svc.GetCollectionSummary()));

app.MapGet("/api/collection/checkQuantity", async (
    string cardName,
    int wantQty,
    bool noProxies,
    bool sparesOnly,
    ICollectionTrackingService svc) =>
{
    var result = await svc.CheckQuantityShortfallAsync(cardName, wantQty, noProxies, sparesOnly);
    return TypedResults.Ok(result);
});

// ── Prices ────────────────────────────────────────────────────────────────────

app.MapGet("/api/prices/sku/{skuId:guid}", async (
    Guid skuId,
    string? currency,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var (price, provider) = await svc.GetLatestPriceForSkuAsync(skuId, currency ?? "USD", cancel);
    return TypedResults.Ok(new SkuPriceResult(price, provider));
});

app.MapGet("/api/prices/sku/{skuId:guid}/history", async (
    Guid skuId,
    string? currency,
    ICollectionTrackingService svc,
    CancellationToken cancel) =>
{
    var history = await svc.GetPriceHistoryForSkuAsync(skuId, currency ?? "USD", cancel);
    return TypedResults.Ok(history);
});

app.MapPost("/api/prices/lowest", async (
    LowestPriceCheckOptions opts,
    ICollectionTrackingService svc,
    IScryfallApiClient scryfallApi,
    CancellationToken cancel) =>
{
    var result = await svc.GetLowestPricesAsync(opts, scryfallApi, cancel);
    return TypedResults.Ok(result);
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
    return TypedResults.Ok(isEmpty);
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
// Request/response records are defined in MtgCollectionTracker.Core.Model and shared
// with the generated API client. Only server-internal types are defined here.

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
