using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public interface IMultiModeCardListBehaviorHost
{
    void HandleBusyChanged(bool oldValue, bool newValue);
}

public interface ICardSkuItem
{
    int RealQty { get; }

    int ProxyQty { get; }

    string SwitchLabel { get; }

    IRelayCommand SwitchFaceCommand { get; }

    Task<Bitmap?> CardImageLarge { get; }

    bool IsCardImageLargeLoading { get; }

    bool IsDoubleFaced { get; }

    string CardName { get; }

    string Edition { get; }

    string? CastingCost { get; }

    string? OracleText { get; }

    string? CardType { get; }

    string? PT { get; }
}

public partial class MultiModeCardListBehavior<T> : ObservableObject where T : class, ICardSkuItem
{
    readonly IMultiModeCardListBehaviorHost _parent;
    INotifyPropertyChanged? _selectedItemNotifier;

    public MultiModeCardListBehavior(IMultiModeCardListBehaviorHost parent)
    {
        _parent = parent;
        this.SelectedItems.CollectionChanged += SelectedCardSkus_CollectionChanged;
    }

    public Task<Bitmap?> SelectedCardImageLarge => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].CardImageLarge : Task.FromResult<Bitmap?>(null);

    public bool SelectedCardImageLargeLoading => this.SelectedItems.Count > 0
        && this.SelectedItems[0].IsCardImageLargeLoading;

    public bool SelectedIsDoubleFaced => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].IsDoubleFaced : false;

    public string? SelectedSwitchLabel => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].SwitchLabel : null;

    public IRelayCommand? SelectedSwitchFaceCommand => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].SwitchFaceCommand : null;

    public string? SelectedCardName => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].CardName : null;

    public string? SelectedEdition => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].Edition : null;

    public string? SelectedCastingCost => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].CastingCost : null;

    public string? SelectedOracleText => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].OracleText : null;

    public string? SelectedCardType => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].CardType : null;

    public string? SelectedPT => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].PT : null;

    private void SelectedCardSkus_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshSelectedItemSubscription();
        this.HasSelectedCardSku = this.SelectedItems.Count == 1;
        this.HasAtLeastOneSelectedCardSku = this.SelectedItems.Count > 0;
        OnPropertyChanged(nameof(SelectedCardImageLargeLoading));
    }

    private void RefreshSelectedItemSubscription()
    {
        if (_selectedItemNotifier != null)
        {
            _selectedItemNotifier.PropertyChanged -= OnSelectedItemPropertyChanged;
            _selectedItemNotifier = null;
        }

        if (this.SelectedItems.Count > 0 && this.SelectedItems[0] is INotifyPropertyChanged notifier)
        {
            _selectedItemNotifier = notifier;
            _selectedItemNotifier.PropertyChanged += OnSelectedItemPropertyChanged;
        }
    }

    private void OnSelectedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICardSkuItem.IsCardImageLargeLoading))
        {
            OnPropertyChanged(nameof(SelectedCardImageLargeLoading));
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAtLeastOneSelectedItem))]
    [NotifyPropertyChangedFor(nameof(IsItemSplittable))]
    private bool _isBusy = false;

    partial void OnIsBusyChanged(bool oldValue, bool newValue)
    {
        _parent.HandleBusyChanged(oldValue, newValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemMergeable))]
    private bool _hasMultipleSelectedCardSkus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAtLeastOneSelectedItem))]
    private bool _hasAtLeastOneSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSplittable))]
    [NotifyPropertyChangedFor(nameof(HasOneSelectedItem))]
    [NotifyPropertyChangedFor(nameof(SelectedCardImageLarge))]
    [NotifyPropertyChangedFor(nameof(SelectedIsDoubleFaced))]
    [NotifyPropertyChangedFor(nameof(SelectedSwitchLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedSwitchFaceCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedCardName))]
    [NotifyPropertyChangedFor(nameof(SelectedEdition))]
    [NotifyPropertyChangedFor(nameof(SelectedCastingCost))]
    [NotifyPropertyChangedFor(nameof(SelectedOracleText))]
    [NotifyPropertyChangedFor(nameof(SelectedCardType))]
    [NotifyPropertyChangedFor(nameof(SelectedPT))]
    [NotifyPropertyChangedFor(nameof(SelectedCardImageLargeLoading))]
    private bool _hasSelectedCardSku;

    public bool IsItemMergeable => !this.IsBusy && this.HasMultipleSelectedCardSkus;
    public bool IsItemSplittable => !this.IsBusy && this.HasSelectedCardSku && (this.SelectedItems[0].RealQty > 1 || this.SelectedItems[0].ProxyQty > 1);
    public bool HasAtLeastOneSelectedItem => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool HasOneSelectedItem => !this.IsBusy && this.HasSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListMode))]
    [NotifyPropertyChangedFor(nameof(IsTableMode))]
    private CardItemViewMode _viewMode;

    public bool IsListMode => this.ViewMode == CardItemViewMode.VisualList;

    public bool IsTableMode => this.ViewMode == CardItemViewMode.Table;

    // Table-specific bound property
    [ObservableProperty]
    private T? _selectedRow;

    partial void OnSelectedRowChanged(T? oldValue, T? newValue)
    {
        // Sync with SelectedCardSkus to ensure existing bound commands work.
        // NOTE: Can only sync one item as Avalonia DataGrid SelectedItems is currently not bindable :(
        SelectedItems.Clear();
        if (newValue != null)
            SelectedItems.Add(newValue);
    }

    public ObservableCollection<T> SelectedItems { get; } = new();
}
