namespace MtgCollectionTracker.Core.Model;

public abstract class DeckModelBase
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string DeckName { get; init; }

    public string? ContainerName { get; set; }

    public string? Format { get; set; }
}

public class DeckInfoModel : DeckModelBase
{
    public required CardSkuModel[] Cards { get; set; }
}
