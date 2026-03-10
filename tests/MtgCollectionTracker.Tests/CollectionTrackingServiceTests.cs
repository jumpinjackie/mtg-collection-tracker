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

    // ── Color filter tests ──────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the database with a small set of card skus that have Scryfall metadata
    /// so color and card-type filter tests can exercise the in-memory filtering logic.
    /// Returns the IDs of the seeded skus in insertion order.
    /// </summary>
    private void SeedCardsWithMetadata(CardsDbContext ctx)
    {
        // Red instant
        var sfBolt = new ScryfallCardMetadata { Id = "sf-bolt", CardName = "Lightning Bolt", Edition = "M10", CardType = "Instant", Rarity = "common", Colors = ["R"] };
        var bolt = new CardSku { CardName = "Lightning Bolt", Edition = "M10", Quantity = 4, ScryfallId = "sf-bolt", Scryfall = sfBolt };

        // Blue instant
        var sfCounterspell = new ScryfallCardMetadata { Id = "sf-counter", CardName = "Counterspell", Edition = "6ED", CardType = "Instant", Rarity = "uncommon", Colors = ["U"] };
        var counterspell = new CardSku { CardName = "Counterspell", Edition = "6ED", Quantity = 4, ScryfallId = "sf-counter", Scryfall = sfCounterspell };

        // White/Blue/Black instant (tricolor)
        var sfStormbind = new ScryfallCardMetadata { Id = "sf-sbb", CardName = "Absorb", Edition = "INV", CardType = "Instant", Rarity = "rare", Colors = ["W", "U", "B"] };
        var absorb = new CardSku { CardName = "Absorb", Edition = "INV", Quantity = 2, ScryfallId = "sf-sbb", Scryfall = sfStormbind };

        // Colorless artifact creature
        var sfColossus = new ScryfallCardMetadata { Id = "sf-walker", CardName = "Darksteel Colossus", Edition = "DST", CardType = "Artifact Creature — Golem", Rarity = "rare", Colors = [] };
        var walker = new CardSku { CardName = "Darksteel Colossus", Edition = "DST", Quantity = 1, ScryfallId = "sf-walker", Scryfall = sfColossus };

        // Green sorcery
        var sfRampant = new ScryfallCardMetadata { Id = "sf-rampant", CardName = "Rampant Growth", Edition = "M10", CardType = "Sorcery", Rarity = "common", Colors = ["G"] };
        var rampant = new CardSku { CardName = "Rampant Growth", Edition = "M10", Quantity = 4, ScryfallId = "sf-rampant", Scryfall = sfRampant };

        // Land (colorless, type Land)
        var sfIsland = new ScryfallCardMetadata { Id = "sf-island", CardName = "Island", Edition = "M10", CardType = "Basic Land — Island", Rarity = "common", Colors = [] };
        var island = new CardSku { CardName = "Island", Edition = "M10", Quantity = 10, ScryfallId = "sf-island", Scryfall = sfIsland };

        // Black enchantment
        var sfEnch = new ScryfallCardMetadata { Id = "sf-necropotence", CardName = "Necropotence", Edition = "ICE", CardType = "Enchantment", Rarity = "rare", Colors = ["B"] };
        var necropotence = new CardSku { CardName = "Necropotence", Edition = "ICE", Quantity = 1, ScryfallId = "sf-necropotence", Scryfall = sfEnch };

        ctx.Cards.AddRange(bolt, counterspell, absorb, walker, rampant, island, necropotence);
        ctx.SaveChanges();
    }

    [Fact]
    public void GetCards_ColorFilter_ReturnsOnlyRedCards()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        var results = service.GetCards(new CardQueryModel { Colors = ["R"], IncludeScryfallMetadata = true }).ToList();

        Assert.Single(results);
        Assert.Equal("Lightning Bolt", results[0].CardName);
    }

    [Fact]
    public void GetCards_ColorFilter_ReturnsBlueAndWhiteBlueCards()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // Asking for U should return Counterspell (mono-U) and Absorb (W/U/B)
        var results = service.GetCards(new CardQueryModel { Colors = ["U"], IncludeScryfallMetadata = true }).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CardName == "Counterspell");
        Assert.Contains(results, r => r.CardName == "Absorb");
    }

    [Fact]
    public void GetCards_ColorFilter_MultipleColors_ReturnsUnionOfMatchingCards()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // R + G should return Lightning Bolt and Rampant Growth
        var results = service.GetCards(new CardQueryModel { Colors = ["R", "G"], IncludeScryfallMetadata = true }).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CardName == "Lightning Bolt");
        Assert.Contains(results, r => r.CardName == "Rampant Growth");
    }

    [Fact]
    public void GetCards_ColorFilter_Colorless_ReturnsArtifactAndLand()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // Colorless (C) matches cards with empty Colors array
        var results = service.GetCards(new CardQueryModel { Colors = ["C"], IncludeScryfallMetadata = true }).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CardName == "Darksteel Colossus");
        Assert.Contains(results, r => r.CardName == "Island");
    }

    [Fact]
    public void GetCards_ColorFilter_ColorlessAndColored_ReturnsBothColorlessAndMatchingColoredCards()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // C + R: colorless cards + red cards
        var results = service.GetCards(new CardQueryModel { Colors = ["C", "R"], IncludeScryfallMetadata = true }).ToList();

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.CardName == "Darksteel Colossus");
        Assert.Contains(results, r => r.CardName == "Island");
        Assert.Contains(results, r => r.CardName == "Lightning Bolt");
    }

    [Fact]
    public void GetCards_ColorFilter_Null_ReturnsAllCardsRegardlessOfColor()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        var results = service.GetCards(new CardQueryModel { SearchFilter = "Lightning Bolt", IncludeScryfallMetadata = true }).ToList();

        Assert.Single(results);
        Assert.Equal("Lightning Bolt", results[0].CardName);
    }

    // ── Card type filter tests ──────────────────────────────────────────────────

    [Fact]
    public void GetCards_CardTypeFilter_ReturnsOnlyInstants()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        var results = service.GetCards(new CardQueryModel { CardTypes = ["Instant"], IncludeScryfallMetadata = true }).ToList();

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.CardName == "Absorb");
        Assert.Contains(results, r => r.CardName == "Counterspell");
        Assert.Contains(results, r => r.CardName == "Lightning Bolt");
    }

    [Fact]
    public void GetCards_CardTypeFilter_ReturnsSorceries()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        var results = service.GetCards(new CardQueryModel { CardTypes = ["Sorcery"], IncludeScryfallMetadata = true }).ToList();

        Assert.Single(results);
        Assert.Equal("Rampant Growth", results[0].CardName);
    }

    [Fact]
    public void GetCards_CardTypeFilter_ReturnsCreatureAndArtifact_ViaPartialTypeLineMatch()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // "Artifact" matches "Artifact Creature — Golem"
        var results = service.GetCards(new CardQueryModel { CardTypes = ["Artifact"], IncludeScryfallMetadata = true }).ToList();

        Assert.Single(results);
        Assert.Equal("Darksteel Colossus", results[0].CardName);
    }

    [Fact]
    public void GetCards_CardTypeFilter_MultipleTypes_ReturnsUnionOfMatchingCards()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // Instant + Enchantment
        var results = service.GetCards(new CardQueryModel { CardTypes = ["Instant", "Enchantment"], IncludeScryfallMetadata = true }).ToList();

        Assert.Equal(4, results.Count);
        Assert.Contains(results, r => r.CardName == "Absorb");
        Assert.Contains(results, r => r.CardName == "Counterspell");
        Assert.Contains(results, r => r.CardName == "Lightning Bolt");
        Assert.Contains(results, r => r.CardName == "Necropotence");
    }

    [Fact]
    public void GetCards_CardTypeFilter_TypeMatchIsCaseInsensitive()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        var results = service.GetCards(new CardQueryModel { CardTypes = ["enchantment"], IncludeScryfallMetadata = true }).ToList();

        Assert.Single(results);
        Assert.Equal("Necropotence", results[0].CardName);
    }

    // ── Combined color + type filter tests ────────────────────────────────────

    [Fact]
    public void GetCards_ColorAndTypeFilter_ReturnsOnlyMatchingIntersection()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // Blue Instants: Counterspell and Absorb
        var results = service.GetCards(new CardQueryModel
        {
            Colors = ["U"],
            CardTypes = ["Instant"],
            IncludeScryfallMetadata = true
        }).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CardName == "Counterspell");
        Assert.Contains(results, r => r.CardName == "Absorb");
    }

    [Fact]
    public void GetCards_ColorAndTypeFilter_ReturnsEmpty_WhenNoCardsMatch()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // There are no Red Sorceries in the seed data
        var results = service.GetCards(new CardQueryModel
        {
            Colors = ["R"],
            CardTypes = ["Sorcery"],
            IncludeScryfallMetadata = true
        }).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void GetCards_ColorAndTypeFilter_WithSearchText_NarrowsResults()
    {
        using var ctx = new CardsDbContext(_dbOptions);
        SeedCardsWithMetadata(ctx);

        var service = CreateService();
        // Search "bolt" + Red + Instant → only Lightning Bolt
        var results = service.GetCards(new CardQueryModel
        {
            SearchFilter = "bolt",
            Colors = ["R"],
            CardTypes = ["Instant"],
            IncludeScryfallMetadata = true
        }).ToList();

        Assert.Single(results);
        Assert.Equal("Lightning Bolt", results[0].CardName);
    }
}
