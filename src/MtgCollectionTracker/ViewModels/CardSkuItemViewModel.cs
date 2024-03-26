using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;

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
    private string _condition = CardCondition.NearMint.ToString();

    [ObservableProperty]
    private string? _comments;

    [ObservableProperty]
    private Bitmap? _cardImage;

    [ObservableProperty]
    private string? _containerName;

    [ObservableProperty]
    private string? _deckName;

    public bool HasContainer => !string.IsNullOrEmpty(this.ContainerName);

    public bool HasDeck => !string.IsNullOrEmpty(this.DeckName);

    public CardSkuItemViewModel WithData(CardSkuModel sku)
    {
        this.CardName = sku.Edition == "PROXY" ? "[Proxy] " + sku.CardName : sku.CardName;
        if (sku.Edition != "PROXY")
            this.Edition = sku.Edition;
        else
            this.Edition = string.Empty;
        this.Condition = (sku.Condition ?? CardCondition.NearMint).ToString();
        this.Quantity = $"Qty: {sku.Quantity}";
        this.Language = sku.Language?.Length > 0 ? sku.Language : "EN";
        this.Comments = sku.Comments;
        this.ContainerName = sku.ContainerName;
        this.DeckName = sku.DeckName;
        return this;
    }
}