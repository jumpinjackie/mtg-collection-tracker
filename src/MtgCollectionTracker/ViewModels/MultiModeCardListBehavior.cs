using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public interface IMultiModeCardListBehaviorHost
{
    void HandleBusyChanged(bool oldValue, bool newValue);
}

public partial class MultiModeCardListBehavior : ObservableObject
{
    readonly IMultiModeCardListBehaviorHost _parent;

    public MultiModeCardListBehavior(IMultiModeCardListBehaviorHost parent)
    {
        _parent = parent;
        this.SelectedCardSkus.CollectionChanged += SelectedCardSkus_CollectionChanged;
    }

    private void SelectedCardSkus_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.HasSelectedCardSku = this.SelectedCardSkus.Count == 1;
        this.HasAtLeastOneSelectedCardSku = this.SelectedCardSkus.Count > 0;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToContainer))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToDeck))]
    [NotifyPropertyChangedFor(nameof(CanUpdateMetadata))]
    [NotifyPropertyChangedFor(nameof(CanSplitCardSku))]
    private bool _isBusy = false;

    partial void OnIsBusyChanged(bool oldValue, bool newValue)
    {
        _parent.HandleBusyChanged(oldValue, newValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCombineCardSkus))]
    private bool _hasMultipleSelectedCardSkus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToContainer))]
    [NotifyPropertyChangedFor(nameof(CanSendSkusToDeck))]
    [NotifyPropertyChangedFor(nameof(CanUpdateMetadata))]
    private bool _hasAtLeastOneSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplitCardSku))]
    private bool _hasSelectedCardSku;

    public bool CanCombineCardSkus => !this.IsBusy && this.HasMultipleSelectedCardSkus;
    public bool CanSplitCardSku => !this.IsBusy && this.HasSelectedCardSku && (this.SelectedCardSkus[0].RealQty > 1 || this.SelectedCardSkus[0].ProxyQty > 1);
    public bool CanSendSkusToContainer => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool CanSendSkusToDeck => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;
    public bool CanUpdateMetadata => !this.IsBusy && this.HasAtLeastOneSelectedCardSku;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListMode))]
    [NotifyPropertyChangedFor(nameof(IsTableMode))]
    private CardItemViewMode _viewMode;

    public bool IsListMode => this.ViewMode == CardItemViewMode.VisualList;

    public bool IsTableMode => this.ViewMode == CardItemViewMode.Table;

    // Table-specific bound property
    [ObservableProperty]
    private CardSkuItemViewModel? _selectedRow;

    partial void OnSelectedRowChanged(CardSkuItemViewModel? oldValue, CardSkuItemViewModel newValue)
    {
        // Sync with SelectedCardSkus to ensure existing bound commands work.
        // NOTE: Can only sync one item as Avalonia DataGrid SelectedItems is currently not bindable :(
        SelectedCardSkus.Clear();
        if (newValue != null)
            SelectedCardSkus.Add(newValue);
    }

    public ObservableCollection<CardSkuItemViewModel> SelectedCardSkus { get; } = new();
}
