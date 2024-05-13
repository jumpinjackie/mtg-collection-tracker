using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.Services.Messaging;

internal class ContainerCreatedMessage
{
    public required ContainerSummaryModel Container { get; set; }
}
