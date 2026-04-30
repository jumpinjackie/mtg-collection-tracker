using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// ViewModel for the reusable <c>CardDetailsPanelView</c> user control.
/// Delegates all display properties to a supplied <see cref="ICardSkuItem"/>
/// and keeps them live by forwarding <see cref="INotifyPropertyChanged"/> events.
/// </summary>
public partial class CardDetailsPanelViewModel : ObservableObject
{
    private static readonly Task<Bitmap?> s_noImage = Task.FromResult<Bitmap?>(null);

    private ICardSkuItem? _item;
    private INotifyPropertyChanged? _itemNotifier;

    /// <summary>Whether a card item is currently set; controls panel visibility.</summary>
    [ObservableProperty]
    private bool _hasCard;

    /// <summary>Large card image, delegated to the current item.</summary>
    public Task<Bitmap?> CardImageLarge => _item?.CardImageLarge ?? s_noImage;

    /// <summary>Whether the large card image is currently loading.</summary>
    public bool IsCardImageLargeLoading => _item?.IsCardImageLargeLoading ?? false;

    /// <summary>Whether the card has a second face.</summary>
    public bool IsDoubleFaced => _item?.IsDoubleFaced ?? false;

    /// <summary>Label for the face-switch button.</summary>
    public string? SwitchLabel => _item?.SwitchLabel;

    /// <summary>Command that flips to the other face of a double-faced card.</summary>
    public IRelayCommand? SwitchFaceCommand => _item?.SwitchFaceCommand;

    /// <summary>Card name.</summary>
    public string? CardName => _item?.CardName;

    /// <summary>Edition / set code.</summary>
    public string? Edition => _item?.Edition;

    /// <summary>Mana cost string.</summary>
    public string? CastingCost => _item?.CastingCost;

    /// <summary>Oracle text.</summary>
    public string? OracleText => _item?.OracleText;

    /// <summary>Type line.</summary>
    public string? CardType => _item?.CardType;

    /// <summary>Power/toughness string (e.g. "3/3"), or <see langword="null"/> if not applicable.</summary>
    public string? PT => _item?.PT;

    /// <summary>
    /// Sets or clears the card item displayed by this panel.
    /// Pass <see langword="null"/> to hide the panel.
    /// </summary>
    public void SetItem(ICardSkuItem? item)
    {
        if (_itemNotifier != null)
        {
            _itemNotifier.PropertyChanged -= OnItemPropertyChanged;
            _itemNotifier = null;
        }

        _item = item;
        HasCard = item != null;

        if (item is INotifyPropertyChanged notifier)
        {
            _itemNotifier = notifier;
            _itemNotifier.PropertyChanged += OnItemPropertyChanged;
        }

        RaiseAllPropertyChanges();
    }

    private void RaiseAllPropertyChanges()
    {
        OnPropertyChanged(nameof(CardImageLarge));
        OnPropertyChanged(nameof(IsCardImageLargeLoading));
        OnPropertyChanged(nameof(IsDoubleFaced));
        OnPropertyChanged(nameof(SwitchLabel));
        OnPropertyChanged(nameof(SwitchFaceCommand));
        OnPropertyChanged(nameof(CardName));
        OnPropertyChanged(nameof(Edition));
        OnPropertyChanged(nameof(CastingCost));
        OnPropertyChanged(nameof(OracleText));
        OnPropertyChanged(nameof(CardType));
        OnPropertyChanged(nameof(PT));
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ICardSkuItem.IsCardImageLargeLoading):
                OnPropertyChanged(nameof(IsCardImageLargeLoading));
                break;
            case nameof(ICardSkuItem.CardImageLarge):
                OnPropertyChanged(nameof(CardImageLarge));
                break;
            case nameof(ICardSkuItem.IsDoubleFaced):
                OnPropertyChanged(nameof(IsDoubleFaced));
                break;
            case nameof(ICardSkuItem.SwitchLabel):
                OnPropertyChanged(nameof(SwitchLabel));
                break;
            case nameof(ICardSkuItem.CastingCost):
                OnPropertyChanged(nameof(CastingCost));
                break;
            case nameof(ICardSkuItem.OracleText):
                OnPropertyChanged(nameof(OracleText));
                break;
            case nameof(ICardSkuItem.CardType):
                OnPropertyChanged(nameof(CardType));
                break;
            case nameof(ICardSkuItem.PT):
                OnPropertyChanged(nameof(PT));
                break;
            case nameof(ICardSkuItem.CardName):
                OnPropertyChanged(nameof(CardName));
                break;
            case nameof(ICardSkuItem.Edition):
                OnPropertyChanged(nameof(Edition));
                break;
        }
    }
}
