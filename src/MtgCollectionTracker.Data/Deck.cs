using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

public class Deck
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(32)]
    public string? Format { get; set; }

    public int? ContainerId { get; set; }

    public virtual Container? Container { get; set; }

    public virtual ICollection<CardSku> Cards { get; set; }
}
