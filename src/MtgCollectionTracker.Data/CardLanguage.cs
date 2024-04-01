using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

public class CardLanguage
{
    [Key]
    public required string Code { get; set; }

    [MaxLength(3)]
    public string? PrintedCode { get; set; }

    [MaxLength(20)]
    public required string Name { get; set; }
}
