using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    bool IsDoubleFaced { get; }
}

public partial class MultiModeCardListBehavior<T> : ObservableObject where T : class, ICardSkuItem
{
    readonly IMultiModeCardListBehaviorHost _parent;

    public MultiModeCardListBehavior(IMultiModeCardListBehaviorHost parent)
    {
        _parent = parent;
        this.SelectedItems.CollectionChanged += SelectedCardSkus_CollectionChanged;
    }

    public Task<Bitmap?> SelectedCardImageLarge => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].CardImageLarge : Task.FromResult<Bitmap?>(null);

    public bool SelectedIsDoubleFaced => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].IsDoubleFaced : false;

    public string? SelectedSwitchLabel => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].SwitchLabel : null;

    public IRelayCommand? SelectedSwitchFaceCommand => this.SelectedItems.Count > 0
        ? this.SelectedItems[0].SwitchFaceCommand : null;

    private void SelectedCardSkus_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.HasSelectedCardSku = this.SelectedItems.Count == 1;
        this.HasAtLeastOneSelectedCardSku = this.SelectedItems.Count > 0;
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

    partial void OnSelectedRowChanged(T? oldValue, T newValue)
    {
        // Sync with SelectedCardSkus to ensure existing bound commands work.
        // NOTE: Can only sync one item as Avalonia DataGrid SelectedItems is currently not bindable :(
        SelectedItems.Clear();
        if (newValue != null)
            SelectedItems.Add(newValue);
    }

    public ObservableCollection<T> SelectedItems { get; } = new();
}
