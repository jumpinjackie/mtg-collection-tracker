using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistItemViewModel : ViewModelBase, ICardSkuItem
{
    public int Id { get; private set; }

    [ObservableProperty]
    private string _cardName = "CARDNAME";

    [ObservableProperty]
    private string _edition = "ED";

    [ObservableProperty]
    private string? _language = "EN";

    [ObservableProperty]
    private string _quantity = "Qty: 1";

    [ObservableProperty]
    private int _quantityNum = 1;

    [ObservableProperty]
    private string _condition = CardCondition.NearMint.ToString();

    [ObservableProperty]
    private string? _comments;

    [ObservableProperty]
    private Bitmap? _cardImage;


    private Bitmap? _frontFaceImage;

    //TODO: Figure out if it's possible to "flip" the front face image to its
    //back face image. Right now this is unused
    private Bitmap? _backFaceImage;

    [ObservableProperty]
    private bool _isFrontFace;

    [ObservableProperty]
    private bool _isDoubleFaced;

    [ObservableProperty]
    private bool _isFoil;

    [ObservableProperty]
    private string _switchLabel = "Switch to Back";

    private void SwitchToFront()
    {
        this.CardImage = _frontFaceImage;
        this.IsFrontFace = true;
        this.SwitchLabel = "Switch to Back";
    }

    private void SwitchToBack()
    {
        this.CardImage = _backFaceImage;
        this.IsFrontFace = false;
        this.SwitchLabel = "Switch to Front";
    }

    [RelayCommand]
    private void SwitchFace()
    {
        if (this.IsFrontFace)
        {
            SwitchToBack();
        }
        else
        {
            SwitchToFront();
        }
    }

    public List<VendorOfferViewModel> Offers { get; set; }

    public string? CollectorNumber { get; set; }

    public string? OriginalCardName { get; set; }

    public string? OriginalEdition { get; set; }

    public int ProxyQty { get; private set; }
    public int RealQty { get; private set; }

    [ObservableProperty]
    private string _lowest = "$?";

    [ObservableProperty]
    private string _highest = "$?";

    [ObservableProperty]
    private string _bestTotal = "$?";

    [ObservableProperty]
    private string _bestVendors = "none";

    [ObservableProperty]
    private string? _vendorExplanation;

    public WishlistItemViewModel WithData(WishlistItemModel item)
    {
        this.Id = item.Id;
        this.IsDoubleFaced = item.IsDoubleFaced;
        this.CollectorNumber = item.CollectorNumber;
        this.OriginalCardName = item.CardName;
        this.OriginalEdition = item.Edition;
        this.IsFoil = item.IsFoil;
        this.CardName = item.Edition == "PROXY" ? "[Proxy] " + item.CardName : item.CardName;
        if (item.Edition != "PROXY")
        {
            this.Edition = item.Edition;
            this.RealQty = item.Quantity;
        }
        else
        {
            this.Edition = string.Empty;
            this.ProxyQty = item.Quantity;
        }
        this.Condition = (item.Condition ?? CardCondition.NearMint).ToString();
        this.Quantity = $"Qty: {item.Quantity}";
        this.QuantityNum = item.Quantity;
        this.Language = item.Language?.Length > 0 ? item.Language : "en";
        this.Offers = item.Offers.Select(o => new VendorOfferViewModel
        {
            AvailableStock = o.AvailableStock,
            Price = o.Price,
            Notes = o.Notes,
            Vendor = new VendorViewModel
            {
                Id = o.VendorId,
                Name = o.VendorName
            }
        }).ToList();
        this.Offers.Sort((a, b) => a.Price.CompareTo(b.Price));

        if (item.Offers.Count > 0)
        {
            this.Lowest = $"${item.Offers.Min(o => o.Price)}";
            this.Highest = $"${item.Offers.Max(o => o.Price)}";

            var (total, vendors, isComplete) = item.Offers.ComputeBestPrice(item.Quantity);
            this.BestTotal = $"${total}";
            this.BestVendors = vendors.Count > 1
                ? "Multiple Vendors"
                : vendors.Count == 1 ? vendors[0].Name : "none";

            var selVendors = vendors.Select(v => v.Name).ToHashSet();
            this.VendorExplanation = ExplainOffers(selVendors, this.Offers);
        }
        else
        {
            this.Lowest = "$?";
            this.Highest = "$?";
            this.BestTotal = "$?";
            this.BestVendors = "none";
            this.VendorExplanation = null;
        }

        if (item.ImageSmall != null)
        {
            using var ms = new MemoryStream(item.ImageSmall);
            _frontFaceImage = new Bitmap(ms);
        }
        if (item.BackImageSmall != null)
        {
            using var ms = new MemoryStream(item.BackImageSmall);
            _backFaceImage = new Bitmap(ms);
        }
        this.SwitchToFront();
        return this;
    }

    private string ExplainOffers(HashSet<string> selectedVendors, List<VendorOfferViewModel> offers)
    {
        var text = new StringBuilder();
        foreach (var offer in offers)
        {
            text.AppendLine($"{(selectedVendors.Contains(offer.Vendor.Name) ? "* " : "  ")}{offer.Vendor.Name} - {offer.AvailableStock} @ ${offer.Price}{(!string.IsNullOrWhiteSpace(offer.Notes) ? (" (" + offer.Notes + ")") : string.Empty)}");
        }
        return text.ToString();
    }
}
