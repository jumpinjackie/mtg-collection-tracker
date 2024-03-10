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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardSku>().HasIndex(nameof(CardSku.CardName));
        modelBuilder.Entity<Container>().HasIndex(nameof(Container.Name)).IsUnique();
        modelBuilder.Entity<Deck>().HasIndex(nameof(Deck.Name)).IsUnique();
    }
}
