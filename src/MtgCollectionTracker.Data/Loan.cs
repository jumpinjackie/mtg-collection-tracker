namespace MtgCollectionTracker.Data;

/// <summary>
/// A <see cref="Loan"/> represents a temporary exchange of cards from a given deck/container
/// to another deck
/// </summary>
public class Loan
{
    public int Id { get; set; }

    public int ToDeckId { get; set; }

    /// <summary>
    /// The deck whose cards are being temporarily exchanged
    /// </summary>
    public virtual Deck? ToDeck { get; set; }

    /// <summary>
    /// The actual card exchanges
    /// </summary>
    public virtual ICollection<LoanExchange> Exchanges { get; set; } = [];

    public async ValueTask ValidateAsync(CardsDbContext db)
    {
        foreach (var exc in this.Exchanges)
        {
            await exc.ValidateAsync(this.ToDeckId, db);
        }
    }
}

public class LoanExchange
{
    public int Id { get; set; }

    public int CardId { get; set; }

    /// <summary>
    /// The card from the <see cref="Loan.ToDeck"/> that this exchange is being temporarily replaced with
    /// </summary>
    public virtual CardSku? Card { get; set; }

    public int LoanId { get; set; }

    /// <summary>
    /// The <see cref="Loan"/> this exchange is part of
    /// </summary>
    public virtual Loan? Loan { get; set; }

    /// <summary>
    /// The cards that will temporarily replace the <see cref="Card"/> in this exchange. The quantities of
    /// the <see cref="Card" /> and the combined replacements here must match. Also their card names must
    /// match as well. None of the cards in this collection must be in the same deck as the <see cref="Card"/>
    /// </summary>
    public virtual ICollection<CardSku> ReplacedWithCards { get; set; } = [];

    public async ValueTask ValidateAsync(int deckId, CardsDbContext db)
    {
        if (this.Card == null)
            await db.Entry(this).Reference(exc => exc.Card).LoadAsync();

        if (this.Card.DeckId != deckId)
            throw new Exception($"Invalid card specified in this exchange. It does not belong to the expected deck (expect: {deckId}, got: {this.Card.DeckId})");

        var replacementSum = ReplacedWithCards.Sum(c => c.Quantity);
        if (this.Card.Quantity != replacementSum)
            throw new Exception($"Invalid exchange, totals are not equal. The card (qty: {this.Card.Quantity}) is being replaced with ({replacementSum}) cards");

        var replacementNames = ReplacedWithCards.Select(c => c.CardName).ToHashSet();
        if (replacementNames.Count > 1)
            throw new Exception($"Invalid exchange. Replacement cards have multiple names (expect: {this.Card.CardName}, got: {string.Join(", ", replacementNames)})");

        if (!replacementNames.Contains(this.Card.CardName))
            throw new Exception($"Invalid exchange. Replacement cards have different names (expect: {this.Card.CardName}, got: {string.Join(", ", replacementNames)})");
    }
}