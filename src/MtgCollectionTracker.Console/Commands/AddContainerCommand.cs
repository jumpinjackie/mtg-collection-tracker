using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("add-container", HelpText = "Adds a container")]
internal class AddContainerCommand : CommandBase
{
    [Option("name", Required = true)]
    public required string Name { get; set; }

    [Option("description", Required = false)]
    public string? Description { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var res = await service.CreateContainerAsync(this.Name, this.Description);

        Stdout($"Added container ({res.Name}, id: {res.Id})");

        return 0;
    }
}
