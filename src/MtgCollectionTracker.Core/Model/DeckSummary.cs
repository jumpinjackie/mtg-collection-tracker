namespace MtgCollectionTracker.Core.Model;

public class DeckSummary
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string ContainerName { get; set; }

    public string? Format { get; set; }

    public int MaindeckTotal { get; set; }

    public int SideboardTotal { get; set; }
}
