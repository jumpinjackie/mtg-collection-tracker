using MtgCollectionTracker.Core.Model;
using System.Collections.Generic;

namespace MtgCollectionTracker.Services.Messaging;

internal class CardsAddedToWishlistMessage
{
    public required IEnumerable<WishlistItemModel> Added { get; init; }
}
