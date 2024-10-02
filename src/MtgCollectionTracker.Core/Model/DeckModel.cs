using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core.Model;

public class DeckCardModel
{
    public int SkuId { get; set; }

    public string CardName { get; set; }

    public string Type { get; set; }

    public int ManaValue { get; set; }

    public string Edition { get; set; }

    public bool IsLand { get; set; }

    public bool IsDoubleFaced { get; set; }

    public string? ScryfallId { get; internal set; }

    public DeckCardModel WithSkuId(int id)
    {
        return new DeckCardModel
        {
            SkuId = id,
            CardName = this.CardName,
            Type = this.Type,
            ManaValue = this.ManaValue,
            Edition = this.Edition,
            IsLand = this.IsLand,
            IsDoubleFaced = this.IsDoubleFaced,
            ScryfallId = this.ScryfallId
        };
    }
}

public record CardSlotImpl(int Quantity, string CardName, string Edition, bool IsLand, bool IsSideboard) : IDeckPrintableSlot;

public class DeckModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public string? Format { get; set; }

    public List<DeckCardModel> MainDeck { get; set; }

    public List<DeckCardModel> Sideboard { get; set; }

    public IEnumerable<IDeckPrintableSlot> GetCards()
    {
        foreach (var grp in this.MainDeck.GroupBy(c => c.CardName))
        {
            var card = grp.First();
            yield return new CardSlotImpl(grp.Count(), card.CardName, card.Edition, card.IsLand, false);
        }
        foreach (var grp in this.Sideboard.GroupBy(c => c.CardName))
        {
            var card = grp.First();
            yield return new CardSlotImpl(grp.Count(), card.CardName, card.Edition, card.IsLand, true);
        }
    }
}
