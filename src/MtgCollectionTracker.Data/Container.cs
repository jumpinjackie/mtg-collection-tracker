using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// A container physically houses cards. This could be a binder or a deck case or 
/// it could be a shoebox :)
/// 
/// This does not define a deck of cards. That is handled by <see cref="Deck"/>
/// </summary>
public class Container
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public virtual ICollection<Deck> Decks { get; set; }

    public virtual ICollection<CardSku> Cards { get; set; }
}
