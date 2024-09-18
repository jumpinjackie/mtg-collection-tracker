using Microsoft.EntityFrameworkCore;

namespace MtgCollectionTracker.Data;

public class CardsDbContext : DbContext
{
    public CardsDbContext(DbContextOptions<CardsDbContext> options)
        : base(options)
    { }

    public DbSet<CardSku> Cards { get; set; }

    public DbSet<Container> Containers { get; set; }

    public DbSet<Deck> Decks { get; set; }

    public DbSet<Vendor> Vendors { get; set; }

    public DbSet<WishlistItem> WishlistItems { get; set; }

    public DbSet<Notes> Notes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardSku>().HasIndex(nameof(CardSku.CardName));
        modelBuilder.Entity<Container>().HasIndex(nameof(Container.Name)).IsUnique();
        modelBuilder.Entity<Deck>().HasIndex(nameof(Deck.Name)).IsUnique();
        modelBuilder.Entity<ScryfallCardMetadata>().HasIndex(nameof(ScryfallCardMetadata.CardName), nameof(ScryfallCardMetadata.Edition), nameof(ScryfallCardMetadata.Language), nameof(ScryfallCardMetadata.CollectorNumber));

        modelBuilder.Entity<CardLanguage>().HasData(
            new CardLanguage { Code = "en", PrintedCode = "en", Name = "English" },
            new CardLanguage { Code = "es", PrintedCode = "sp", Name = "Spanish" },
            new CardLanguage { Code = "fr", PrintedCode = "fr", Name = "French" },
            new CardLanguage { Code = "de", PrintedCode = "de", Name = "German" },
            new CardLanguage { Code = "it", PrintedCode = "it", Name = "Italian" },
            new CardLanguage { Code = "pt", PrintedCode = "pt", Name = "Portuguese" },
            new CardLanguage { Code = "ja", PrintedCode = "jp", Name = "Japanese" },
            new CardLanguage { Code = "ko", PrintedCode = "kr", Name = "Korean" },
            new CardLanguage { Code = "ru", PrintedCode = "ru", Name = "Russian" },
            new CardLanguage { Code = "zhs", PrintedCode = "cs", Name = "Simplified Chinese" },
            new CardLanguage { Code = "zht", PrintedCode = "ct", Name = "Traditional Chinese" },
            new CardLanguage { Code = "he", Name = "Hebrew" },
            new CardLanguage { Code = "la", Name = "Latin" },
            new CardLanguage { Code = "grc", Name = "Ancient Greek" },
            new CardLanguage { Code = "ar", Name = "Arabic" },
            new CardLanguage { Code = "sa", Name = "Sanskrit" },
            new CardLanguage { Code = "ph", PrintedCode = "ph", Name = "Phyrexian" }
        );
    }
}
