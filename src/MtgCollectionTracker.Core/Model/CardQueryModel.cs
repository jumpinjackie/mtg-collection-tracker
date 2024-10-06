namespace MtgCollectionTracker.Core.Model;

public class CardQueryModel
{
    public string? SearchFilter { get; set; }

    public IEnumerable<int>? CardSkuIds { get; set; }

    public int[]? DeckIds { get; set; }

    public int[]? ContainerIds { get; set; }

    public IEnumerable<string>? Tags { get; set; }

    public bool NotInDecks { get; set; }

    public bool NoProxies { get; set; }

    public bool UnParented { get; set; }

    public bool MissingMetadata { get; set; }

    public bool IncludeScryfallMetadata { get; set; }
}
