using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using StrongInject;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for <see cref="CollectionTrackingService"/>.
/// Uses an in-memory SQLite database to exercise database-backed methods.
/// </summary>
public class CollectionTrackingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CardsDbContext> _dbOptions;

    public CollectionTrackingServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<CardsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CardsDbContext(_dbOptions);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private CollectionTrackingService CreateService()
    {
        Func<Owned<CardsDbContext>> dbFactory = () =>
        {
            var ctx = new CardsDbContext(_dbOptions);
            return new Owned<CardsDbContext>(ctx, () => ctx.Dispose());
        };

        var mockFs = new Mock<ICardImageFileSystem>();
        var mockClient = new Mock<ScryfallApi.Client.IScryfallApiClient>();
        var cache = new CardImageCache(dbFactory, mockFs.Object, mockClient.Object);
        var priceCache = new PriceCache();

        return new CollectionTrackingService(dbFactory, cache, priceCache);
    }

    [Theory]
    [InlineData("Plains")]
    [InlineData("Island")]
    [InlineData("Swamp")]
    [InlineData("Mountain")]
    [InlineData("Forest")]
    [InlineData("Snow-Covered Plains")]
    [InlineData("Snow-Covered Island")]
    [InlineData("Snow-Covered Swamp")]
    [InlineData("Snow-Covered Mountain")]
    [InlineData("Snow-Covered Forest")]
    [InlineData("Wastes")]
    public void IsBasicLand_ReturnsTrue_ForBasicLandNames(string cardName)
    {
        var service = CreateService();
        Assert.True(service.IsBasicLand(cardName));
    }

    [Theory]
    [InlineData("Lightning Bolt")]
    [InlineData("Black Lotus")]
    [InlineData("Birds of Paradise")]
    [InlineData("Tundra")]
    [InlineData("Dark Ritual")]
    public void IsBasicLand_ReturnsFalse_ForNonBasicLands(string cardName)
    {
        var service = CreateService();
        Assert.False(service.IsBasicLand(cardName));
    }

    [Fact]
    public void GetCollectionSummary_ReturnsAllZeros_ForEmptyDatabase()
    {
        var service = CreateService();
        var summary = service.GetCollectionSummary();

        Assert.Equal(0, summary.CardTotal);
        Assert.Equal(0, summary.ProxyTotal);
        Assert.Equal(0, summary.SkuTotal);
        Assert.Equal(0, summary.DeckTotal);
        Assert.Equal(0, summary.ContainerTotal);
    }

    [Fact]
    public async Task CreateContainerAsync_CreatesAndReturnsContainer()
    {
        var service = CreateService();
        var container = await service.CreateContainerAsync("Test Box", "A test container");

        Assert.NotEqual(0, container.Id);
        Assert.Equal("Test Box", container.Name);
        Assert.Equal("A test container", container.Description);
    }

    [Fact]
    public void GetContainers_ReturnsCreatedContainers()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        ctx.Containers.Add(new MtgCollectionTracker.Data.Container { Name = "Binder 1" });
        ctx.Containers.Add(new MtgCollectionTracker.Data.Container { Name = "Binder 2" });
        ctx.SaveChanges();

        var service = CreateService();
        var containers = service.GetContainers().ToList();

        Assert.Equal(2, containers.Count);
        Assert.Contains(containers, c => c.Name == "Binder 1");
        Assert.Contains(containers, c => c.Name == "Binder 2");
    }

    [Fact]
    public async Task CreateDeckAsync_CreatesAndReturnsDeck()
    {
        var service = CreateService();
        var deck = await service.CreateDeckAsync("Legacy Burn", "Legacy", null);

        Assert.NotEqual(0, deck.Id);
        Assert.Equal("Legacy Burn", deck.DeckName);
        Assert.Equal("Legacy", deck.Format);
        Assert.Equal("[Legacy] Legacy Burn", deck.Name);
    }

    [Fact]
    public void GetDecks_ReturnsAllDecks()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        ctx.Decks.Add(new Deck { Name = "Deck A", Format = "Modern" });
        ctx.Decks.Add(new Deck { Name = "Deck B", Format = "Legacy" });
        ctx.SaveChanges();

        var service = CreateService();
        var decks = service.GetDecks(null).ToList();

        Assert.Equal(2, decks.Count);
        Assert.Contains(decks, d => d.DeckName == "Deck A");
        Assert.Contains(decks, d => d.DeckName == "Deck B");
    }

    [Fact]
    public async Task DismantleDeckAsync_WithNoContainer_ReturnsCardsToUnparented()
    {
        var service = CreateService();

        // Create a deck and add two card SKUs to it
        var deck = await service.CreateDeckAsync("Legacy Burn", "Legacy", null);
        await service.AddToDeckOrContainerAsync(null, deck.Id, new() { CardName = "Lightning Bolt", Edition = "M10", Quantity = 4 });
        await service.AddToDeckOrContainerAsync(null, deck.Id, new() { CardName = "Mountain", Edition = "M10", Quantity = 16 });

        var result = await service.DismantleDeckAsync(new() { DeckId = deck.Id, ContainerId = null });

        Assert.Equal(20, result.Removed);
        Assert.Null(result.ContainerName);

        // Deck should be gone
        var decks = service.GetDecks(null).ToList();
        Assert.DoesNotContain(decks, d => d.Id == deck.Id);

        // Cards should be unparented (no container, no deck)
        var cards = service.GetCards(new MtgCollectionTracker.Core.Model.CardQueryModel()).ToList();
        Assert.All(cards, c => Assert.Null(c.ContainerName));
        Assert.All(cards, c => Assert.Null(c.DeckName));
    }

    [Fact]
    public async Task DismantleDeckAsync_WithContainer_ReturnsCardsToContainer()
    {
        var service = CreateService();

        // Create a container, a deck and add card SKUs to the deck
        var container = await service.CreateContainerAsync("Main Binder", null);
        var deck = await service.CreateDeckAsync("Legacy Burn", "Legacy", null);
        await service.AddToDeckOrContainerAsync(null, deck.Id, new() { CardName = "Lightning Bolt", Edition = "M10", Quantity = 4 });
        await service.AddToDeckOrContainerAsync(null, deck.Id, new() { CardName = "Mountain", Edition = "M10", Quantity = 16 });

        var result = await service.DismantleDeckAsync(new() { DeckId = deck.Id, ContainerId = container.Id });

        Assert.Equal(20, result.Removed);
        Assert.Equal("Main Binder", result.ContainerName);

        // Deck should be gone
        var decks = service.GetDecks(null).ToList();
        Assert.DoesNotContain(decks, d => d.Id == deck.Id);

        // Cards should be in the specified container with no deck
        var cards = service.GetCards(new MtgCollectionTracker.Core.Model.CardQueryModel()).ToList();
        Assert.All(cards, c => Assert.StartsWith("Main Binder", c.ContainerName));
        Assert.All(cards, c => Assert.Null(c.DeckName));
    }

    [Fact]
    public async Task DismantleDeckAsync_ClearsSideboardFlag()
    {
        var service = CreateService();

        var deck = await service.CreateDeckAsync("Vintage Deck", "Vintage", null);
        await service.AddToDeckOrContainerAsync(null, deck.Id, new() { CardName = "Black Lotus", Edition = "LEA", Quantity = 1, IsSideboard = true });

        await service.DismantleDeckAsync(new() { DeckId = deck.Id, ContainerId = null });

        // The card should no longer be flagged as sideboard
        var cards = service.GetCards(new MtgCollectionTracker.Core.Model.CardQueryModel()).ToList();
        Assert.Single(cards);
        Assert.False(cards[0].IsSideboard);
    }

    [Fact]
    public async Task DismantleDeckAsync_InvalidDeckId_ThrowsException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() =>
            service.DismantleDeckAsync(new() { DeckId = 9999, ContainerId = null }).AsTask());
    }

    [Fact]
    public async Task DismantleDeckAsync_InvalidContainerId_ThrowsException()
    {
        var service = CreateService();
        var deck = await service.CreateDeckAsync("Test Deck", "Modern", null);

        await Assert.ThrowsAsync<Exception>(() =>
            service.DismantleDeckAsync(new() { DeckId = deck.Id, ContainerId = 9999 }).AsTask());
    }
}
