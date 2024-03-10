using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.Console.Commands;

[Verb("add-to-container", HelpText = "Add cards to a container")]
internal class AddToContainerCommand : CommandBase
{
    [Option("quantity", Required = true)]
    public int Quantity { get; set; }

    [Option("card-name", Required = true)]
    public required string CardName { get; set; }

    [Option("edition", Required = true)]
    public required string Edition { get; set; }

    [Option("container-id", Required = true)]
    public int ContainerId { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();

        var res = await service.AddToContainerAsync(this.ContainerId, new()
        {
            Quantity = this.Quantity,
            CardName = this.CardName,
            Edition = this.Edition
        });

        Stdout($"Added [{res.Quantity}x {res.CardName}, id: {res.Id}] to [{res.ContainerName}]");

        return 0;
    }
}
