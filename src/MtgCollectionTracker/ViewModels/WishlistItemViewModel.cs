﻿using Avalonia.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistItemViewModel : ViewModelBase
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

    public WishlistItemViewModel WithData(WishlistItemModel item)
    {
        this.Id = item.Id;
        this.IsDoubleFaced = item.IsDoubleFaced;
        this.CollectorNumber = item.CollectorNumber;
        this.OriginalCardName = item.CardName;
        this.OriginalEdition = item.Edition;
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
        this.Language = item.Language?.Length > 0 ? item.Language : "en";
        this.Offers = item.Offers.Select(o => new VendorOfferViewModel
        {
            AvailableStock = o.AvailableStock,
            Price = o.Price,
            Vendor = new VendorViewModel
            {
                Id = o.VendorId,
                Name = o.VendorName
            }
        }).ToList();

        if (item.Offers.Count > 0)
        {
            this.Lowest = $"${item.Offers.Min(o => o.Price)}";
            this.Highest = $"${item.Offers.Max(o => o.Price)}";

            var (total, vendors, isComplete) = item.Offers.ComputeBestPrice(item.Quantity);
            this.BestTotal = $"${total}";
            this.BestVendors = vendors.Count > 1
                ? "Multiple Vendors"
                : vendors.Count == 1 ? vendors[0] : "none";
        }
        else
        {
            this.Lowest = "$?";
            this.Highest = "$?";
            this.BestTotal = "$?";
            this.BestVendors = "none";
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
}
