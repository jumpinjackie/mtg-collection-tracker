using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Messaging;

internal class TagsAppliedMessage
{
    public required List<string> CurrentTags { get; set; }
}
