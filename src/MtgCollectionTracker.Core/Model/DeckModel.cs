namespace MtgCollectionTracker.Core.Model;

public class DeckModel
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string ContainerName { get; set; }

    public string? Format { get; set; }

    public required CardSkuModel[] Cards { get; set; }
}