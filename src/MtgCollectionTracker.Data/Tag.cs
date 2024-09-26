using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

public class Tag
{
    public int Id { get; set; }

    [MaxLength(48)]
    public required string Name { get; set; }
}
