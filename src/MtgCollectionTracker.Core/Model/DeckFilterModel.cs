namespace MtgCollectionTracker.Core.Model;

public class DeckFilterModel
{
    public IEnumerable<int>? Ids { get; set; }

    public required IEnumerable<string> Formats { get; set; }
}
