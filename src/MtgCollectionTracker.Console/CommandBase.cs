using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Console;

internal abstract class CommandBase
{
    public void Stderr(string msg) => System.Console.Error.WriteLine(msg);

    public void Stdout(string msg) => System.Console.WriteLine(msg);

    public async ValueTask<int> ExecuteAsync()
    {
        try
        {
            var serviceProvider = new ServiceCollection()
                .AddDbContext<CardsDbContext>(o => o.UseSqlite("Data Source=collection.sqlite"), ServiceLifetime.Transient)
                .AddTransient<CollectionTrackingService>()
                .BuildServiceProvider();

            await using (var db = serviceProvider.GetRequiredService<CardsDbContext>())
            {
                //Stdout("Creating database and applying migrations if required");
                await db.Database.MigrateAsync();
            }

            var res = await ExecuteInternalAsync(serviceProvider);
            return res;
        }
        catch (Exception ex)
        {
            Stderr($"ERROR: {ex.Message}");
            return 1;
        }
    }

    protected abstract ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider);
}
