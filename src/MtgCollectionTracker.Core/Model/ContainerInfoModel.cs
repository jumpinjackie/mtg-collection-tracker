namespace MtgCollectionTracker.Core.Model;

public class ContainerInfoModel
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }
}
