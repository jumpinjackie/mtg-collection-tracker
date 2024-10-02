using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Stubs;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class CardVisualViewModel : ViewModelBase, ICardSkuItem, ISendableCardItem
{
    readonly ICollectionTrackingService _service;

    public CardVisualViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
    }

    public CardVisualViewModel(ICollectionTrackingService service)
    {
        _service = service;
    }

    public int Id { get; set; }

    public string? ScryfallId { get; set; }

    public bool IsDoubleFaced { get; set; }

    [ObservableProperty]
    private bool _isFrontFace;

    [ObservableProperty]
    private string _switchLabel = "Switch to Back";

    [ObservableProperty]
    private Task<Bitmap?> _cardImage;

    public Task<Bitmap?> FrontFaceImageSmall => GetSmallFrontFaceImageAsync();

    public Task<Bitmap?> BackFaceImageSmall => GetSmallBackFaceImageAsync();

    private async Task<Bitmap?> GetSmallFrontFaceImageAsync()
    {
        if (this.ScryfallId != null)
        {
            using var stream = await _service.GetSmallFrontFaceImageAsync(this.ScryfallId);
            return new Bitmap(stream);
        }
        return null;
    }

    private async Task<Bitmap?> GetSmallBackFaceImageAsync()
    {
        if (this.ScryfallId != null)
        {
            using var stream = await _service.GetSmallBackFaceImageAsync(this.ScryfallId);
            return new Bitmap(stream);
        }
        return null;
    }

    [ObservableProperty]
    private Task<Bitmap?> _cardImageLarge;

    public Task<Bitmap?> FrontFaceImageLarge => GetLargeFrontFaceImageAsync();

    public Task<Bitmap?> BackFaceImageLarge => GetLargeBackFaceImageAsync();

    private async Task<Bitmap?> GetLargeFrontFaceImageAsync()
    {
        if (this.ScryfallId != null)
        {
            using var stream = await _service.GetLargeFrontFaceImageAsync(this.ScryfallId);
            return new Bitmap(stream);
        }
        return null;
    }

    private async Task<Bitmap?> GetLargeBackFaceImageAsync()
    {
        if (this.ScryfallId != null)
        {
            using var stream = await _service.GetLargeBackFaceImageAsync(this.ScryfallId);
            return new Bitmap(stream);
        }
        return null;
    }

    internal void SwitchToFront()
    {
        this.CardImage = this.FrontFaceImageSmall;
        this.CardImageLarge = this.FrontFaceImageLarge;
        this.IsFrontFace = true;
        this.SwitchLabel = "Switch to Back";
    }

    private void SwitchToBack()
    {
        this.CardImage = this.BackFaceImageSmall;
        this.CardImageLarge = this.BackFaceImageLarge;
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

    public required string CardName { get; set; }

    public string Tooltip => (IsProxy ? ("PROXY: " + CardName) : CardName) + $" ({Quantity}x)";

    public bool IsLand { get; set; }

    public int Quantity { get; set; }

    public string? Type { get; set; }

    public bool IsProxy { get; set; }

    public bool IsGrouped { get; set; }

    public bool IsSideboard { get; set; }

    public string CardNameBgColor => IsProxy ? "RosyBrown" : "Gray";

    public int ProxyQty { get; private set; }

    public int RealQty { get; private set; }

    public string Edition { get; set; }

    public string? CastingCost { get; set; }

    public string? OracleText { get; set; }

    public string? CardType { get; set; }

    public string? PT { get; set; }

    public string[]? Colors { get; set; }

    public string[]? ColorIdentity { get; set; }

    public CardVisualViewModel ApplyQuantities()
    {
        if (IsProxy)
            this.ProxyQty = this.Quantity;
        else
            this.RealQty = this.Quantity;
        return this;
    }

    public CardVisualViewModel ApplyScryfallMetadata(DeckCardModel sku)
    {
        this.CastingCost = sku.CastingCost;
        this.ColorIdentity = sku.ColorIdentity;
        this.Colors = sku.Colors;
        this.CardType = sku.CardType;
        this.OracleText = sku.OracleText;
        if (sku.Power != null && sku.Toughness != null)
        {
            this.PT = sku.Power + "/" + sku.Toughness;
        }

        return this;
    }
}
