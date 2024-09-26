using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

public class Tag
{
    [Key]
    [MaxLength(48)]
    public required string Name { get; set; }
}
