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
}
