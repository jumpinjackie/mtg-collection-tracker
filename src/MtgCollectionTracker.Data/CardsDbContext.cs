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

    public DbSet<ScryfallIdMapping> ScryfallIdMappings { get; set; }
    public DbSet<CardPricingEntry> CardPricingEntries { get; set; }
    public DbSet<CardPricingDownloadHistory> CardPricingDownloadHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardSku>().HasIndex(nameof(CardSku.CardName));
        modelBuilder.Entity<Container>().HasIndex(nameof(Container.Name)).IsUnique();
        modelBuilder.Entity<Deck>().HasIndex(nameof(Deck.Name)).IsUnique();
        modelBuilder.Entity<Deck>()
            .HasOne(d => d.BannerCard)
            .WithMany()
            .HasForeignKey(d => d.BannerCardId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        modelBuilder.Entity<Deck>()
            .HasOne(d => d.Commander)
            .WithMany()
            .HasForeignKey(d => d.CommanderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        modelBuilder.Entity<Tag>().HasIndex(nameof(Tag.Name)).IsUnique();
        modelBuilder.Entity<CardSku>()
            .OwnsMany(c => c.Tags, cb =>
            {
                cb.HasKey("Id");
                cb.HasIndex(t => t.Name);
                cb.Property(t => t.Name).IsRequired();
            });
        modelBuilder.Entity<WishlistItem>()
            .OwnsMany(w => w.Tags, cb =>
            {
                cb.HasKey("Id");
                cb.HasIndex(t => t.Name);
                cb.Property(t => t.Name).IsRequired();
            });
        modelBuilder.Entity<ScryfallCardMetadata>().HasIndex(nameof(ScryfallCardMetadata.CardName), nameof(ScryfallCardMetadata.Edition), nameof(ScryfallCardMetadata.Language), nameof(ScryfallCardMetadata.CollectorNumber));

        modelBuilder.Entity<ScryfallIdMapping>().HasKey(s => s.ScryfallId);
        modelBuilder.Entity<ScryfallIdMapping>().HasIndex(s => s.MtgJsonUuid).IsUnique();

        // Keep these as POCO navigation properties only and avoid a hard FK between
        // Scryfall metadata and identifier mappings. Existing databases may contain
        // metadata before mappings are imported.
        modelBuilder.Entity<ScryfallCardMetadata>().Ignore(m => m.ScryfallIdMapping);
        modelBuilder.Entity<ScryfallIdMapping>().Ignore(m => m.ScryfallCardMetadata);

        modelBuilder.Entity<CardPricingEntry>().HasIndex(e => e.Uuid);
        modelBuilder.Entity<CardPricingEntry>().HasIndex(e => new { e.Uuid, e.CardFinish, e.Currency, e.ProviderListing });
        modelBuilder.Entity<CardPricingEntry>()
            .HasOne(e => e.ScryfallIdMapping)
            .WithMany(m => m.CardPricingEntries)
            .HasForeignKey(e => e.Uuid)
            .HasPrincipalKey(m => m.MtgJsonUuid)
            .OnDelete(DeleteBehavior.NoAction);

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
