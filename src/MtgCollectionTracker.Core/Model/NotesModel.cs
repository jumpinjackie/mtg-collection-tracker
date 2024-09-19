namespace MtgCollectionTracker.Core.Model;

public class NotesModel
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public required string Notes { get; set; }
}
