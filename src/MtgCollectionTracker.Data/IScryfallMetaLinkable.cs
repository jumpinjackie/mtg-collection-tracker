namespace MtgCollectionTracker.Data;

public interface IScryfallMetaLinkable
{
    string CardName { get; }

    string Edition { get; }

    string? LanguageId { get; }

    string? CollectorNumber { get; }

    ScryfallCardMetadata? Scryfall { get; set; }

    bool IsLand { get; set; }
}
