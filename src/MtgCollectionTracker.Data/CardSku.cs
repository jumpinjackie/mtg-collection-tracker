using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

/// <summary>
/// A <see cref="CardSku"/> defines a specific quantity of a particular card in a 
/// particular condition.
/// 
/// <see cref="CardSku"/> instances may belong to a <see cref="Container"/> or a <see cref="Deck"/>
/// or both
/// </summary>
public class CardSku : IScryfallMetaLinkable, IDeckPrintableSlot
{
    public int Id { get; set; }

    /// <summary>
    /// The quantity of cards
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The name of the card
    /// </summary>
    [MaxLength(256)]
    public required string CardName { get; set; }

    /// <summary>
    /// The code for the card's edition
    /// </summary>
    [MaxLength(5)]
    public required string Edition { get; set; }

    /// <summary>
    /// The languages of this quantity of cards. If not specified, it is assumed to be in English
    /// </summary>
    [MaxLength(3)]
    public string? LanguageId { get; set; }

    public virtual CardLanguage? Language { get; set; }

    /// <summary>
    /// The collector number
    /// </summary>
    [MaxLength(5)]
    public string? CollectorNumber { get; set; }

    /// <summary>
    /// The scryfall card id, used to associate with auxilliary information like card image, market price, etc
    /// </summary>
    public string? ScryfallId { get; set; }

    public virtual ScryfallCardMetadata? Scryfall { get; set; }

    public int? DeckId { get; set; }

    /// <summary>
    /// The parent <see cref="Deck"/> this quantity of cards belongs to
    /// </summary>
    public virtual Deck? Deck { get; set; }

    public int? ContainerId { get; set; }

    /// <summary>
    /// The parent <see cref="Container"/> this quantity of cards belongs to
    /// </summary>
    public virtual Container? Container { get; set; }

    /// <summary>
    /// Comments about this quantity of cards. Use this for things like describing conditions
    /// of cards, whether it is signed, or any other relveant notes.
    /// </summary>
    [MaxLength(256)]
    public string? Comments { get; set; }

    /// <summary>
    /// If this quantity of cards is part of a <see cref="Deck"/>, indicates if this is part of the
    /// sideboard
    /// </summary>
    public bool IsSideboard { get; set; }

    /// <summary>
    /// Indicates if this quantity of cards is foil
    /// </summary>
    public bool IsFoil { get; set; }

    /// <summary>
    /// Indicates the condition of this quantity of cards. If not specified it assumed to be
    /// <see cref="CardCondition.NearMint"/>
    /// </summary>
    public CardCondition? Condition { get; set; }

    /// <summary>
    /// Indicates if this card is a land. This is to assist in deck stats and decklist generation/presentation
    /// </summary>
    public bool IsLand { get; set; }

    /// <summary>
    /// Tags applied for this card
    /// </summary>
    public ICollection<CardSkuTag> Tags { get; } = new List<CardSkuTag>();

    public bool SyncTags(ICollection<string> tags)
    {
        bool bDirty = false;
        var toAdd = new List<CardSkuTag>();
        var toRemove = new List<CardSkuTag>();
        foreach (var inTag in tags)
        {
            if (!this.Tags.Any(t => t.Name == inTag))
            {
                toAdd.Add(new CardSkuTag { Name = inTag });
            }
        }
        foreach (var currentTag in this.Tags)
        {
            if (!tags.Any(t => t == currentTag.Name))
            {
                toRemove.Add(currentTag);
            }
        }
        if (toRemove.Count > 0)
        {
            foreach (var r in toRemove)
            {
                this.Tags.Remove(r);
                bDirty = true;
            }
        }
        foreach (var a in toAdd)
        {
            this.Tags.Add(a);
            bDirty = true;
        }
        return bDirty;
    }

    /// <summary>
    /// Creates a new <see cref="CardSku"/> with the specified quantity
    /// </summary>
    /// <param name="quantity"></param>
    /// <returns>This instance if the <paramref name="quantity"/> is the same as <see cref="Quantity"/>. Otherwise a clone of this instance is returned with the specified quantity and the amount is subtracted from <see cref="Quantity"/> of this instance</returns>
    public CardSku RemoveQuantity(int quantity)
    {
        // If full quantity, just return this
        if (this.Quantity == quantity)
            return this;

        // Otherwise, subtract this quantity
        this.Quantity -= quantity;

        // Then clone this sku with the new quantity
        var newSku = new CardSku
        {
            Quantity = quantity,

            CardName = this.CardName,
            Comments = this.Comments,
            Condition = this.Condition,
            
            CollectorNumber = this.CollectorNumber,

            Container = this.Container,
            ContainerId = this.ContainerId,
            Deck = this.Deck,
            DeckId = this.DeckId,

            ScryfallId = this.ScryfallId,

            Edition = this.Edition,
            IsFoil = this.IsFoil,
            IsLand = this.IsLand,
            IsSideboard = this.IsSideboard,
            Language = this.Language,
        };

        // Copy tags
        foreach (var t in this.Tags)
        {
            newSku.Tags.Add(new CardSkuTag { Name = t.Name });
        }

        return newSku;
    }
}
