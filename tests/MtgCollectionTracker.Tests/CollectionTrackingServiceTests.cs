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

    // -------------------------------------------------------------------------
    // Tests for: updating a SKU's collector number must not reset its language
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCardSkuByIdAsync_ReturnsCorrectLanguage_ForNonEnglishSku()
    {
        // Arrange — insert a Japanese card SKU directly via the db context
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            ctx.Cards.Add(new CardSku
            {
                CardName = "Lightning Bolt",
                Edition = "M10",
                LanguageId = "ja",
                Quantity = 1
            });
            ctx.SaveChanges();
        }

        // Act
        var service = CreateService();
        int id;
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            id = ctx.Cards.Single().Id;
        }

        var model = await service.GetCardSkuByIdAsync(id, CancellationToken.None);

        // Assert — language must be "ja", not the fallback "en"
        Assert.Equal("ja", model.Language);
    }

    [Fact]
    public async Task UpdateCardSkuAsync_WithCollectorNumberOnly_PreservesNonEnglishLanguage()
    {
        // Arrange — insert a Japanese card SKU
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            ctx.Cards.Add(new CardSku
            {
                CardName = "Lightning Bolt",
                Edition = "M10",
                LanguageId = "ja",
                Quantity = 1
            });
            ctx.SaveChanges();
        }

        int id;
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            id = ctx.Cards.Single().Id;
        }

        // Act — update only the collector number, do NOT provide a language
        var service = CreateService();
        await service.UpdateCardSkuAsync(
            new MtgCollectionTracker.Core.Model.UpdateCardSkuInputModel
            {
                Ids = [id],
                CollectorNumber = "123"
            },
            scryfallApiClient: null,
            cancel: CancellationToken.None);

        // Assert — the LanguageId in the database must still be "ja"
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            var sku = ctx.Cards.Single(c => c.Id == id);
            Assert.Equal("ja", sku.LanguageId);
            Assert.Equal("123", sku.CollectorNumber);
        }
    }

    [Fact]
    public async Task UpdateCardSkuAsync_WithCollectorNumberOnly_GetCardSkuByIdReturnsCorrectLanguage()
    {
        // Arrange — insert a Japanese card SKU
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            ctx.Cards.Add(new CardSku
            {
                CardName = "Lightning Bolt",
                Edition = "M10",
                LanguageId = "ja",
                Quantity = 1
            });
            ctx.SaveChanges();
        }

        int id;
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            id = ctx.Cards.Single().Id;
        }

        // Act — update only the collector number
        var service = CreateService();
        await service.UpdateCardSkuAsync(
            new MtgCollectionTracker.Core.Model.UpdateCardSkuInputModel
            {
                Ids = [id],
                CollectorNumber = "123"
            },
            scryfallApiClient: null,
            cancel: CancellationToken.None);

        // Re-fetch via the service (exercises the Include fix in GetCardSkuByIdAsync)
        var model = await service.GetCardSkuByIdAsync(id, CancellationToken.None);

        // Assert — language returned by the service model must be "ja", not the fallback "en"
        Assert.Equal("ja", model.Language);
        Assert.Equal("123", model.CollectorNumber);
    }
}
