namespace MtgCollectionTracker.Core.Model;

public class DeckCardModel
{
    public int SkuId { get; set; }

    public string CardName { get; set; }

    public byte[] FrontFaceImage { get; set; }

    public byte[]? BackFaceImage { get; set; }

    public string Type { get; set; }

    public int ManaValue { get; set; }

    public bool IsProxy { get; set; }

    public bool IsLand { get; set; }
}

public class DeckModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public DeckCardModel[] MainDeck { get; set; }

    public DeckCardModel[] Sideboard { get; set; }
}
