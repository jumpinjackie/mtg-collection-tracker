using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Core;

public static class PublicExtensionMethods
{
    public static (decimal TotalPrice, List<string> Vendors, bool IsComplete) ComputeBestPrice<T>(this IEnumerable<T> offers, int requiredQty)
        where T : IVendorOffer
    {
        var vendors = new List<string>();
        int remainingQty = requiredQty;
        decimal total = 0;
        bool isComplete = false;
        // TODO: If multiple vendors offer the same required amount at the same price, all those
        // vendors should be included, instead of the just the first one we find
        foreach (var offer in offers.OrderBy(o => o.Price))
        {
            if (remainingQty > 0)
            {
                if (offer.AvailableStock >= remainingQty)
                {
                    total += (remainingQty * offer.Price);
                    remainingQty -= remainingQty;
                    vendors.Add(offer.Name);
                }
                else // Take what's left
                {
                    total += (offer.AvailableStock * offer.Price);
                    remainingQty -= offer.AvailableStock;
                    vendors.Add(offer.Name);
                }

                if (remainingQty <= 0)
                {
                    isComplete = true;
                    break;
                }
            }
        }
        return (total, vendors, isComplete);
    }
}

internal static class InternalExtensionMethods
{
    public static async ValueTask ApplyScryfallMetadataAsync(this IScryfallMetaLinkable sku, ScryfallMetadataResolver resolver, bool refetchMetadata, CancellationToken cancel)
    {
        sku.Scryfall = await resolver.TryResolveAsync(sku.CardName, sku.Edition, sku.LanguageId, sku.CollectorNumber, refetchMetadata, cancel);
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
