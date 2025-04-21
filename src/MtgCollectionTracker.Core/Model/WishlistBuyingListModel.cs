using System.Text;

namespace MtgCollectionTracker.Core.Model;

public record BuyingListItem(int Qty, string CardName, decimal? Price, string? Notes);

public class WishlistBuyingListModel
{
    readonly Dictionary<string, List<BuyingListItem>> _itemsByVendor = new();

    public void Add(string vendor, BuyingListItem item)
    {
        if (!_itemsByVendor.ContainsKey(vendor))
        {
            _itemsByVendor[vendor] = new();
        }
        _itemsByVendor[vendor].Add(item);
    }

    public void Write(StringBuilder writer)
    {
        foreach (var kvp in _itemsByVendor)
        {
            writer.AppendLine($"# Vendor: {kvp.Key}");
            writer.AppendLine();
            decimal total = 0;
            foreach (var item in kvp.Value)
            {
                var priceFrag = item.Price.HasValue ? $"@ ${item.Price.Value} " : string.Empty;
                var totalFrag = item.Price.HasValue ? $"[${item.Qty * item.Price.Value}]" : string.Empty;
                if (string.IsNullOrWhiteSpace(item.Notes))
                    writer.AppendLine($"{item.Qty} {item.CardName} {priceFrag}{totalFrag}");
                else
                    writer.AppendLine($"{item.Qty} {item.CardName} {priceFrag}{totalFrag} ({item.Notes})");
                if (item.Price.HasValue)
                    total += (item.Qty * item.Price.Value);
            }
            writer.AppendLine();
            writer.AppendLine($"Total spend: ${total}");
            writer.AppendLine();
        }
    }
}
