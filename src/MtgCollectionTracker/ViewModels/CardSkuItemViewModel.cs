using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private string _quantity = "1";

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
    private string? _containerName;

    [ObservableProperty]
    private string? _deckName;

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

    public string? CollectorNumber { get; set; }

    public string? OriginalCardName { get; set; }

    public string? OriginalEdition { get; set; }

    public bool HasContainer => !string.IsNullOrEmpty(this.ContainerName);

    public bool HasDeck => !string.IsNullOrEmpty(this.DeckName);

    public int ProxyQty { get; private set; }
    public int RealQty { get; private set; }

    public CardSkuItemViewModel WithData(CardSkuModel sku)
    {
        this.Id = sku.Id;
        this.IsDoubleFaced = sku.IsDoubleFaced;
        this.CollectorNumber = sku.CollectorNumber;
        this.OriginalCardName = sku.CardName;
        this.OriginalEdition = sku.Edition;
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
        this.Quantity = $"{sku.Quantity}";
        this.Language = sku.Language?.Length > 0 ? sku.Language : "en";
        this.Comments = sku.Comments;
        this.ContainerName = sku.ContainerName;
        this.DeckName = sku.DeckName;
        if (sku.ImageSmall != null)
        {
            using var ms = new MemoryStream(sku.ImageSmall);
            _frontFaceImage = new Bitmap(ms);
        }
        if (sku.BackImageSmall != null)
        {
            using var ms = new MemoryStream(sku.BackImageSmall);
            _backFaceImage = new Bitmap(ms);
        }
        this.SwitchToFront();
        return this;
    }
}