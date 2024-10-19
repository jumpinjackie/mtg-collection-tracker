using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// Defines a Magic: The Gathering deck
/// 
/// It may optionally be part of a <see cref="Container"/>
/// </summary>
public class Deck
{
    public int Id { get; set; }

    /// <summary>
    /// The name of this deck
    /// </summary>
    [MaxLength(64)]
    public required string Name { get; set; }

    /// <summary>
    /// The format this deck is legal in or intended to be played in
    /// </summary>
    [MaxLength(32)]
    public string? Format { get; set; }

    public int? ContainerId { get; set; }

    /// <summary>
    /// The optional container this deck is part of
    /// </summary>
    public virtual Container? Container { get; set; }

    /// <summary>
    /// The cards in this deck
    /// </summary>
    public virtual ICollection<CardSku> Cards { get; set; }

    /// <summary>
    /// The temporary loans this deck is involved in
    /// </summary>
    public virtual ICollection<TemporaryExchange> Exchanges { get; set; }
}
