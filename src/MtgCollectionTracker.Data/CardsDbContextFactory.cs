using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Data;

public class CardsDbContextFactory : IDesignTimeDbContextFactory<CardsDbContext>
{
    public CardsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CardsDbContext>();
        optionsBuilder.UseSqlite("Data Source=collection.sqlite");

        return new CardsDbContext(optionsBuilder.Options);
    }
}
