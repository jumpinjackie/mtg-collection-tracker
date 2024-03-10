using Microsoft.EntityFrameworkCore;

namespace MtgCollectionTracker.Data;

public class CardsDbContext : DbContext
{
    public DbSet<CardSku> Cards { get; set; }

    public DbSet<Container> Containers { get; set; }

    public DbSet<Deck> Decks { get; set; }
}
