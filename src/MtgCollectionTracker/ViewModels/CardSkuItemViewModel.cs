using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public enum CardItemViewMode
{
    VisualList,
    Table
}

public partial class CardSkuItemViewModel : ViewModelBase, ICardSkuItem, ISendableCardItem
{
    readonly ICollectionTrackingService _service;

    public CardSkuItemViewModel(ICollectionTrackingService service)
    {
        _service = service;
    }

    public CardSkuItemViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _deckName = "Some Deck";
        _containerName = "Some Container";
        this.Comments = "Some comments";
        this.TagList = ["foo", "bar"];
        this.Tags = string.Join(Environment.NewLine, this.TagList);
        this.TagsText = $"{this.TagList.Length} tag(s)";
    }

    public int Id { get; private set; }

    public string? ScryfallId { get; private set; }

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
    private string? _containerName;

    [ObservableProperty]
    private string? _deckName;

    public string? DeckOrContainer => this.ContainerName ?? this.DeckName ?? "<un-assigned>";

    public string? MainDeckOrSideboard => this.IsSideboard ? "Sideboard" : "Main Deck";

    [ObservableProperty]
    private bool _isFrontFace;

    [ObservableProperty]
    private bool _isDoubleFaced;

    [ObservableProperty]
    private bool _isFoil;

    [ObservableProperty]
    private bool _isSideboard;

    [ObservableProperty]
    private string _switchLabel = "Switch to Back";

    private void SwitchToFront()
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

    public string? CollectorNumber { get; set; }

    public string? OriginalCardName { get; set; }

    public string? OriginalEdition { get; set; }

    public string? CastingCost { get; set; }

    public string? OracleText { get; set; }

    public string? CardType { get; set; }

    public string? PT { get; set; }

    public string[]? Colors { get; set; }

    public string[]? ColorIdentity { get; set; }

    public bool HasComments => !string.IsNullOrWhiteSpace(this.Comments);

    public bool HasContainer => !string.IsNullOrEmpty(this.ContainerName);

    public bool HasDeck => !string.IsNullOrEmpty(this.DeckName);

    public int ProxyQty { get; private set; }
    public int RealQty { get; private set; }

    [ObservableProperty]
    private Task<Bitmap?> _cardImageLarge;

    public Task<Bitmap?> FrontFaceImageLarge => GetLargeFrontFaceImageAsync();

    public Task<Bitmap?> BackFaceImageLarge => GetLargeBackFaceImageAsync();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTags))]
    private string _tags;

    [ObservableProperty]
    private string _tagsText;

    public bool HasTags => !string.IsNullOrWhiteSpace(this.Tags);

    public string[] TagList { get; set; }

    int ISendableCardItem.Quantity => CardListPrinter.IsProxyEdition(this.Edition) ? this.ProxyQty : this.RealQty;

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

    public CardSkuItemViewModel WithData(CardSkuModel sku)
    {
        this.Id = sku.Id;
        this.ScryfallId = sku.ScryfallId;

        this.CastingCost = sku.CastingCost;
        this.ColorIdentity = sku.ColorIdentity;
        this.Colors = sku.Colors;
        this.CardType = sku.CardType;
        this.OracleText = sku.OracleText;
        if (sku.Power != null && sku.Toughness != null)
        {
            this.PT = sku.Power + "/" + sku.Toughness;
        }

        this.IsDoubleFaced = sku.IsDoubleFaced;
        this.IsSideboard = sku.IsSideboard;
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
        this.IsFoil = sku.IsFoil;
        this.TagList = sku.Tags;
        this.Tags = string.Join(Environment.NewLine, this.TagList);
        this.TagsText = $"{this.TagList.Length} tag(s)";
        this.SwitchToFront();
        return this;
    }
}