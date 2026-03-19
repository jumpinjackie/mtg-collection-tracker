using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MtgCollectionTracker.Data;

public class CardsDbContextFactory : IDesignTimeDbContextFactory<CardsDbContext>
{
    public CardsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CardsDbContext>();
        optionsBuilder.UseSqlite("Data Source=/home/user/apps/mtg-collection-tracker/collection.sqlite");

        return new CardsDbContext(optionsBuilder.Options);
    }
}
