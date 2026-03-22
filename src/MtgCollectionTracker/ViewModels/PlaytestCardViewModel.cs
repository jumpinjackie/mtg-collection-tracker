using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        Counters.CollectionChanged += OnCountersChanged;
    }

    public PlaytestCardViewModel()
    {
        ThrowIfNotDesignMode();
        _imageCache = null!;
        Counters.CollectionChanged += OnCountersChanged;
    }

    private void OnCountersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasCounters));
    }

    [ObservableProperty]
    private string _cardName = "";

    /// <summary>
    /// The name to display for the current face of the card.
    /// For true double-faced cards (transform/modal DFC), shows only the relevant face name.
    /// Adventure cards and split cards with " // " naming are kept as-is (IsDoubleFaced = false).
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!IsDoubleFaced)
                return CardName;

            var sep = CardName.IndexOf(" // ", System.StringComparison.Ordinal);
            if (sep < 0)
                return CardName;

            return IsFrontFace
                ? CardName[..sep]
                : CardName[(sep + 4)..];
        }
    }

    /// <summary>
    /// Counters placed on this card or token
    /// </summary>
    public ObservableCollection<CardCounterViewModel> Counters { get; } = new();

    /// <summary>
    /// Whether this card has any counters on it
    /// </summary>
    public bool HasCounters => Counters.Count > 0;

    /// <summary>
    /// Whether this card has a mana cost to display
    /// </summary>
    public bool HasManaCost => !string.IsNullOrEmpty(ManaCost);

    [ObservableProperty]
    private string? _scryfallId;

    [ObservableProperty]
    private string? _scryfallIdBack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasManaCost))]
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

    /// <summary>
    /// Whether this card is the commander in a Commander game (always returns to Command Zone on reset)
    /// </summary>
    public bool IsCommanderCard { get; set; }

    [ObservableProperty]
    private bool _isTapped;

    /// <summary>
    /// Whether this card is currently selected on the battlefield (for multi-select actions)
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
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
        IsCommanderCard = card.IsCommanderCard;
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

        using var stream = await _imageCache.GetSmallFrontFaceImageAsync(ScryfallId);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }

    private async Task<Bitmap?> GetSmallBackFaceImageAsync()
    {
        if (ScryfallIdBack == null || !IsDoubleFaced)
            return null;

        using var stream = await _imageCache.GetSmallBackFaceImageAsync(ScryfallIdBack);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }

    private async Task<Bitmap?> GetLargeFrontFaceImageAsync()
    {
        if (ScryfallId == null)
            return null;

        using var stream = await _imageCache.GetLargeFrontFaceImageAsync(ScryfallId);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }

    private async Task<Bitmap?> GetLargeBackFaceImageAsync()
    {
        if (ScryfallIdBack == null || !IsDoubleFaced)
            return null;

        using var stream = await _imageCache.GetLargeBackFaceImageAsync(ScryfallIdBack);
        if (stream == null)
            return null;
        return new Bitmap(stream);
    }
}
