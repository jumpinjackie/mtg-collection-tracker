using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core;

internal static class ExtensionMethods
{
    public static async ValueTask ApplyScryfallMetadataAsync(this CardSku sku, ScryfallMetadataResolver resolver, bool refetchMetadata, CancellationToken cancel)
    {
        sku.Scryfall = await resolver.TryResolveAsync(sku.CardName, sku.Edition, sku.Language, sku.CollectorNumber, refetchMetadata, cancel);
        if (!sku.IsLand)
        {
            sku.IsLand = sku.Scryfall?.CardType?.StartsWith("Land") == true || sku.Scryfall?.CardType?.StartsWith("Basic Land") == true;
        }
    }

    public static Uri? GetFrontFaceImageUri(this ScryfallApi.Client.Models.Card card, string size)
    {
        if (card.CardFaces?.Length > 0)
        {
            if (card.CardFaces[0].ImageUris != null)
            {
                // 1st item is the front face
                if (card.CardFaces[0].ImageUris?.TryGetValue(size, out var uri) == true)
                    return uri;
            }
            else // This is most likely an adventure card, just get from root ImageUris
            {
                if (card.ImageUris?.TryGetValue(size, out var uri) == true)
                    return uri;
            }
        }
        else // Non-DFCs just get from root ImageUris
        {
            if (card.ImageUris?.TryGetValue(size, out var uri) == true)
                return uri;
        }
        return null;
    }

    public static Uri? GetBackFaceImageUri(this ScryfallApi.Client.Models.Card card, string size)
    {
        if (card.CardFaces?.Length == 2)
        {
            // 2nd item is the back face
            if (card.CardFaces[1].ImageUris?.TryGetValue(size, out var uri) == true)
                return uri;
        }
        return null;
    }
}
