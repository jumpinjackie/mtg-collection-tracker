namespace MtgCollectionTracker.Data;

// The design of this class is to allow for the future possibility of multiple notes, but for now the
// app will only create and maintain one instance

public class Notes
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public required string Text { get; set; }
}
