namespace MtgCollectionTracker.Data;

/// <summary>
/// A <see cref="TemporaryExchange"/> represents a temporary loan/exchange of 1 or more cards from a given
/// deck/container to another deck. The common use case is when you need to replace proxies in your deck with
/// real copies from your collection for a sanctioned tournament, which are to be returned back to their
/// original containers/decks when the tournament is over. Another use case is loaning out real cards to
/// other people.
/// </summary>
public class TemporaryExchange
{
    public int Id { get; set; }

    /// <summary>
    /// The name of this exchange (eg. "Exchange for upcoming tourney")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The cards to "loan out" to the <see cref="ToDeck"/>. The cards in question cannot be from
    /// <see cref="ToDeck"/> and any service operations working with this entity should ensure this
    /// is the case
    /// </summary>
    public ICollection<CardSku> Cards { get; set; } = [];

    public int ToDeckId { get; set; }

    public virtual Deck? ToDeck { get; set; }

    /// <summary>
    /// The cards in the deck that are temporarily replaced in this exchange
    /// </summary>
    public ICollection<ExchangedDeckCard> DeckCards { get; set; } = [];
}

public class ExchangedDeckCard
{
    public int Id { get; set; }

    /// <summary>
    /// The quantity of the given <see cref="Card"/> being exchanged
    /// </summary>
    public int Quantity { get; set; }

    public int CardId { get; set; }

    public virtual CardSku? Card { get; set; }

    public int ExchangeId { get; set; }

    public virtual TemporaryExchange? Exchange { get; set; }
}