using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.ViewModels;

public partial class CardSkuItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _cardName = "CARDNAME";

    [ObservableProperty]
    private string _edition = "ED";

    [ObservableProperty]
    private string? _language = "EN";

    [ObservableProperty]
    private string _quantity = "Qty: 1";

    [ObservableProperty]
    private Bitmap? _cardImage;

    public CardSkuItemViewModel WithData(CardSkuModel sku)
    {
        this.CardName = sku.Edition == "PROXY" ? "[Proxy] " + sku.CardName : sku.CardName;
        if (sku.Edition != "PROXY")
            this.Edition = sku.Edition;
        this.Quantity = $"Qty: {sku.Quantity}";
        this.Language = sku.Language?.Length > 0 ? sku.Language : "EN";
        return this;
    }
}