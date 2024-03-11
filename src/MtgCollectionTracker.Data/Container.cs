using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// A container physically houses cards. This could be a binder or a deck case or 
/// it could be a shoebox :)
/// 
/// In addition to holding cards, a container could also hold a series of decks
/// 
/// This however, does not define a deck of cards. That is handled by <see cref="Deck"/>
/// </summary>
public class Container
{
    public int Id { get; set; }

    /// <summary>
    /// The name of this container
    /// </summary>
    [MaxLength(64)]
    public required string Name { get; set; }

    /// <summary>
    /// An optional description of this container
    /// </summary>
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// The decks in this container
    /// </summary>
    public virtual ICollection<Deck> Decks { get; set; }

    /// <summary>
    /// The cards in this container
    /// </summary>
    public virtual ICollection<CardSku> Cards { get; set; }
}
