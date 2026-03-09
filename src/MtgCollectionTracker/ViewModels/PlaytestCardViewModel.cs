using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// ViewModel representing a single card in the playtesting game
/// </summary>
public partial class PlaytestCardViewModel : ViewModelBase
{
    private readonly CardImageCache _imageCache;

    public PlaytestCardViewModel(CardImageCache imageCache)
    {
        _imageCache = imageCache;
    }

    public PlaytestCardViewModel()
    {
        ThrowIfNotDesignMode();
        _imageCache = null!;
    }

    [ObservableProperty]
    private string _cardName = "";

    [ObservableProperty]
    private string? _scryfallId;

    [ObservableProperty]
    private string? _scryfallIdBack;

    [ObservableProperty]
    private string? _manaCost;

    [ObservableProperty]
    private string? _cardType;

    [ObservableProperty]
    private string? _power;

    [ObservableProperty]
    private string? _toughness;

    [ObservableProperty]
    private string? _oracleText;

    [ObservableProperty]
    private bool _isLand;

    [ObservableProperty]
    private bool _isDoubleFaced;

    [ObservableProperty]
    private bool _isToken;

    [ObservableProperty]
    private bool _isTapped;

    [ObservableProperty]
    private bool _isFrontFace = true;

    [ObservableProperty]
    private GameZone _zone;

    [ObservableProperty]
    private Task<Bitmap?>? _cardImage;

    [ObservableProperty]
    private Task<Bitmap?>? _cardImageLarge;

    public string? PT
    {
        get
        {
            if (!string.IsNullOrEmpty(Power) && !string.IsNullOrEmpty(Toughness))
                return $"{Power}/{Toughness}";
            return null;
        }
    }

    partial void OnIsTappedChanged(bool value)
    {
        this.RotationAngle = value ? 90 : 0;
    }

    [ObservableProperty]
    private double _rotationAngle;

    /// <summary>
    /// Initialize the card from a PlaytestCard model
    /// </summary>
    public void InitializeFrom(PlaytestCard card)
    {
        CardName = card.CardName;
        ScryfallId = card.ScryfallId;
        ScryfallIdBack = card.ScryfallIdBack;
        ManaCost = card.ManaCost;
        CardType = card.CardType;
        Power = card.Power;
        Toughness = card.Toughness;
        OracleText = card.OracleText;
        IsLand = card.IsLand;
        IsDoubleFaced = card.IsDoubleFaced;
        IsToken = card.IsToken;
        IsTapped = card.IsTapped;
        IsFrontFace = card.IsFrontFace;
        Zone = card.Zone;

        LoadCardImages();
    }

    /// <summary>
    /// Load the card images from cache
    /// </summary>
    public void LoadCardImages()
    {
        if (IsToken)
        {
            CardImage = Task.FromResult<Bitmap?>(null);
            CardImageLarge = Task.FromResult<Bitmap?>(null);
            return;
        }

        if (IsFrontFace)
        {
            CardImage = GetSmallFrontFaceImageAsync();
            CardImageLarge = GetLargeFrontFaceImageAsync();
        }
        else
        {
            CardImage = GetSmallBackFaceImageAsync();
            CardImageLarge = GetLargeBackFaceImageAsync();
        }
    }

    /// <summary>
    /// Toggle the tapped state
    /// </summary>
    public void ToggleTapped()
    {
        IsTapped = !IsTapped;
        OnPropertyChanged(nameof(RotationAngle));
    }

    /// <summary>
    /// Transform the card (flip to back face)
    /// </summary>
    public void Transform()
    {
        if (!IsDoubleFaced)
            return;

        IsFrontFace = !IsFrontFace;
        LoadCardImages();
    }

    private async Task<Bitmap?> GetSmallFrontFaceImageAsync()
    {
        if (ScryfallId == null)
            return null;

        var stream = await _imageCache.GetSmallFrontFaceImageAsync(ScryfallId);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }

    private async Task<Bitmap?> GetSmallBackFaceImageAsync()
    {
        if (ScryfallIdBack == null || !IsDoubleFaced)
            return null;

        var stream = await _imageCache.GetSmallBackFaceImageAsync(ScryfallIdBack);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }

    private async Task<Bitmap?> GetLargeFrontFaceImageAsync()
    {
        if (ScryfallId == null)
            return null;

        var stream = await _imageCache.GetLargeFrontFaceImageAsync(ScryfallId);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }

    private async Task<Bitmap?> GetLargeBackFaceImageAsync()
    {
        if (ScryfallIdBack == null || !IsDoubleFaced)
            return null;

        var stream = await _imageCache.GetLargeBackFaceImageAsync(ScryfallIdBack);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }
}
