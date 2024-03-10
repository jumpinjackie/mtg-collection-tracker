using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class ContainerSummary
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int Total { get; set; }

    public virtual ICollection<CardSku> Cards { get; set; }
}
