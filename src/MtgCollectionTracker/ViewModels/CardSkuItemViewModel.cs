using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Data;
using System.IO;

namespace MtgCollectionTracker.ViewModels;

public partial class CardSkuItemViewModel : ViewModelBase
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

    [ObservableProperty]
    private string? _containerName;

    [ObservableProperty]
    private string? _deckName;

    public bool HasContainer => !string.IsNullOrEmpty(this.ContainerName);

    public bool HasDeck => !string.IsNullOrEmpty(this.DeckName);

    public int ProxyQty { get; private set; }
    public int RealQty { get; private set; }

    public CardSkuItemViewModel WithData(CardSkuModel sku)
    {
        this.Id = sku.Id;
        this.CardName = sku.Edition == "PROXY" ? "[Proxy] " + sku.CardName : sku.CardName;
        if (sku.Edition != "PROXY")
        {
            this.Edition = sku.Edition;
            this.RealQty = sku.Quantity;
        }
        else
        {
            this.Edition = string.Empty;
            this.ProxyQty = sku.Quantity;
        }
        this.Condition = (sku.Condition ?? CardCondition.NearMint).ToString();
        this.Quantity = $"Qty: {sku.Quantity}";
        this.Language = sku.Language?.Length > 0 ? sku.Language : "EN";
        this.Comments = sku.Comments;
        this.ContainerName = sku.ContainerName;
        this.DeckName = sku.DeckName;
        if (sku.ImageSmall != null)
        {
            using var ms = new MemoryStream(sku.ImageSmall);
            this.CardImage = new Bitmap(ms);
        }
        return this;
    }
}