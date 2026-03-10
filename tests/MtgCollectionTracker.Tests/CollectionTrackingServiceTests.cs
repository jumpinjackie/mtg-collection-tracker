using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MtgCollectionTracker.Core.Model;
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

    [Fact]
    public async Task UpdateWishlistItemAsync_RemovesVendorOffer_WhenOfferNotInUpdatedList()
    {
        // Arrange: seed two vendors and a wishlist item with both vendors' offers
        int itemId;
        int vendor1Id;
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            var vendor1 = new Vendor { Name = "VendorA" };
            var vendor2 = new Vendor { Name = "VendorB" };
            ctx.Vendors.AddRange(vendor1, vendor2);

            var item = new WishlistItem
            {
                CardName = "Lightning Bolt",
                NormalizedCardName = "lightning bolt",
                Edition = "LEA",
                Quantity = 4
            };
            ctx.WishlistItems.Add(item);
            await ctx.SaveChangesAsync();

            ctx.Add(new VendorPrice { ItemId = item.Id, VendorId = vendor1.Id, Price = 1.50m, AvailableStock = 4 });
            ctx.Add(new VendorPrice { ItemId = item.Id, VendorId = vendor2.Id, Price = 2.00m, AvailableStock = 2 });
            await ctx.SaveChangesAsync();

            itemId = item.Id;
            vendor1Id = vendor1.Id;
        }

        var service = CreateService();

        // Act: update the item keeping only VendorA's offer (removing VendorB)
        var updated = await service.UpdateWishlistItemAsync(new UpdateWishlistItemInputModel
        {
            Id = itemId,
            VendorOffers =
            [
                new UpdateVendorOfferInputModel { VendorId = vendor1Id, Price = 1.50m, Available = 4 }
            ]
        }, null, CancellationToken.None);

        // Assert: only VendorA's offer remains
        Assert.Single(updated.Offers);
        Assert.Equal(vendor1Id, updated.Offers[0].VendorId);
    }

    [Fact]
    public async Task UpdateWishlistItemAsync_ClearsAllVendorOffers_WhenEmptyListProvided()
    {
        // Arrange: seed a vendor and a wishlist item with one offer
        int itemId;
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            var vendor = new Vendor { Name = "MegaVendor" };
            ctx.Vendors.Add(vendor);

            var item = new WishlistItem
            {
                CardName = "Dark Ritual",
                NormalizedCardName = "dark ritual",
                Edition = "LEA",
                Quantity = 1
            };
            ctx.WishlistItems.Add(item);
            await ctx.SaveChangesAsync();

            ctx.Add(new VendorPrice { ItemId = item.Id, VendorId = vendor.Id, Price = 5.00m, AvailableStock = 1 });
            await ctx.SaveChangesAsync();

            itemId = item.Id;
        }

        var service = CreateService();

        // Act: update with an empty vendor offers list (user removed all offers)
        var updated = await service.UpdateWishlistItemAsync(new UpdateWishlistItemInputModel
        {
            Id = itemId,
            VendorOffers = []
        }, null, CancellationToken.None);

        // Assert: no offers remain
        Assert.Empty(updated.Offers);
    }

    [Fact]
    public async Task UpdateWishlistItemAsync_RetainsExistingOffers_WhenVendorOffersIsNull()
    {
        // Arrange: seed a vendor and wishlist item with an offer
        int itemId;
        using (var ctx = new CardsDbContext(_dbOptions))
        {
            var vendor = new Vendor { Name = "StableVendor" };
            ctx.Vendors.Add(vendor);

            var item = new WishlistItem
            {
                CardName = "Black Lotus",
                NormalizedCardName = "black lotus",
                Edition = "LEA",
                Quantity = 1
            };
            ctx.WishlistItems.Add(item);
            await ctx.SaveChangesAsync();

            ctx.Add(new VendorPrice { ItemId = item.Id, VendorId = vendor.Id, Price = 9999.99m, AvailableStock = 1 });
            await ctx.SaveChangesAsync();

            itemId = item.Id;
        }

        var service = CreateService();

        // Act: update with VendorOffers = null (apply offers checkbox was NOT ticked)
        var updated = await service.UpdateWishlistItemAsync(new UpdateWishlistItemInputModel
        {
            Id = itemId,
            VendorOffers = null
        }, null, CancellationToken.None);

        // Assert: original offer is untouched
        Assert.Single(updated.Offers);
    }
}
