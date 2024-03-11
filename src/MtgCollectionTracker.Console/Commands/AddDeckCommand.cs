using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("add-deck", HelpText = "Add a new deck to your collection")]
internal class AddDeckCommand : CommandBase
{
    [Option("name", Required = true, HelpText = "The name of the deck")]
    public required string Name { get; set; }

    [Option("format", Required = false, HelpText = "The format that this deck can be played in")]
    public string? Format { get; set; }

    [Option("container-id", Required = false, HelpText = "The optional id of the container this deck belongs to")]
    public int? ContainerId { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var res = await service.CreateDeckAsync(this.Name, this.Format, this.ContainerId);

        Stdout($"Added deck ({res.Name}, id: {res.Id})");

        return 0;
    }
}
