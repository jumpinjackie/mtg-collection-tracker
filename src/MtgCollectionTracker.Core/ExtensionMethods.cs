using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using ScryfallApi.Client.Models;
using System.Net.Http.Json;

namespace MtgCollectionTracker.Core;

public record VendorOffer(string Name, int Qty, decimal Price, string? Notes);

public static class PublicExtensionMethods
{
    // From: https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560
    static int LevenshteinDist(string source1, string source2) //O(n*m)
    {
        var source1Length = source1.Length;
        var source2Length = source2.Length;

        var matrix = new int[source1Length + 1, source2Length + 1];

        // First calculation, if one entry is empty return full length
        if (source1Length == 0)
            return source2Length;

        if (source2Length == 0)
            return source1Length;

        // Initialization of matrix with row size source1Length and columns size source2Length
        for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
        for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

        // Calculate rows and collumns distances
        for (var i = 1; i <= source1Length; i++)
        {
            for (var j = 1; j <= source2Length; j++)
            {
                var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }
        // return result
        return matrix[source1Length, source2Length];
    }

    public static async Task<(bool found, string? name, string? correctEdition, int apiCalls)> CheckCardNameAsync(this IScryfallApiClient client, string name, string? setHint = null)
    {
        int apiCalls = 0;
        // In most cases, fuzzy search should hit it, and fast!
        // NOTE: This API is not covered by our client nuget package, so we'll hit it with the raw HTTP client
        // that's only accessible from our wrapper
        if (client is ScryfallClient wrap)
        {
            var uri = setHint != null ? $"cards/named?fuzzy={name}&set={setHint}" : $"cards/named?fuzzy={name}";
            var resp = await wrap.RawClient.GetAsync(uri);
            apiCalls++;
            if (resp.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var fuzzyMatch = await resp.Content.ReadFromJsonAsync<Card>();
                if (fuzzyMatch?.ObjectType == "card")
                {
                    return (true, fuzzyMatch.Name, fuzzyMatch.Set, apiCalls);
                }
            }
        }

        var allCards = new HashSet<(string name, string set, string releasedAt)>();
        int pageNo = 0;
        while (true)
        {
            pageNo++;
            try
            {
                var sfCards = await client.Cards.Search(name, pageNo, new SearchOptions()
                {
                    IncludeMultilingual = true,
                    Mode = SearchOptions.RollupMode.Prints
                });
                apiCalls++;
                // Only consider paper results. We are managing a *paper*-based card collection after all!
                allCards.UnionWith(sfCards.Data.Where(c => c.Games?.Contains("paper") == true).Select(c => (c.Name, c.Set, c.ReleasedAt)));
                if (!sfCards.HasMore)
                    break;
            }
            catch
            {
                break;
            }
        }

        if (allCards.Count == 0)
        {
            return (false, null, null, apiCalls);
        }

        if (allCards.Count == 1)
            return (true, allCards.First().name, allCards.First().set, apiCalls);

        // In the event of in-exact match, prefer name with shortest levenshtein distance
        if (!string.IsNullOrWhiteSpace(setHint))
        {
            // The set codes will be in lower case, so lower case our input to avoid potential
            // levenshtein false positives due to mismatched casing
            var inSetHint = setHint.ToLower();
            var first = allCards.OrderBy(c => LevenshteinDist(c.name, name)).ThenBy(c => LevenshteinDist(c.set, inSetHint)).ThenBy(c => c.releasedAt).First();
            return (true, first.name, first.set, apiCalls);
        }
        var match = allCards.OrderBy(c => LevenshteinDist(c.name, name)).ThenBy(c => c.releasedAt).First();
        return (true, match.name, match.set, apiCalls);
    }

    public static (decimal TotalPrice, List<VendorOffer> Vendors, bool IsComplete) ComputeBestPrice<T>(this IEnumerable<T> offers, int requiredQty)
        where T : IVendorOffer
    {
        var vendors = new List<VendorOffer>();
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
                    var subQty = remainingQty;
                    remainingQty -= subQty;
                    vendors.Add(new (offer.Name, subQty, offer.Price, offer.Notes));
                }
                else // Take what's left
                {
                    total += (offer.AvailableStock * offer.Price);
                    remainingQty -= offer.AvailableStock;
                    vendors.Add(new (offer.Name, offer.AvailableStock, offer.Price, offer.Notes));
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
        // TODO: This would be point to apply an ascii-folded/normalised card name
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
