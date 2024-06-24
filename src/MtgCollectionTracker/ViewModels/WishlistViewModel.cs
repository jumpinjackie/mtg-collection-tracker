using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistViewModel : RecipientViewModelBase, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public WishlistViewModel()
    {
        this.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
        this.IsActive = true;
    }

    public WishlistViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
        this.SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            var items = _service.GetWishlistItems();
            foreach (var item in items)
            {
                this.Cards.Add(_vmFactory.WishListItem().WithData(item));
            }
            this.ApplySummary();
        }
        base.OnActivated();
    }

    private void SelectedItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.HasSingleSelectedItem = SelectedItems.Count == 1;
        this.HasSelectedItems = SelectedItems.Count >= 1;
    }

    public ObservableCollection<WishlistItemViewModel> Cards { get; } = new();

    public ObservableCollection<WishlistItemViewModel> SelectedItems { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public bool IsEmptyCollection => Cards.Count == 0;

    [ObservableProperty]
    private bool _hasSelectedItems;

    [ObservableProperty]
    private bool _hasSingleSelectedItem;

    [ObservableProperty]
    private string _wishlistSummary;

    [RelayCommand]
    private void AddCards()
    {
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Add Cards to Wishlist", _vmFactory.AddCardsToWishlist())
        });
    }

    [RelayCommand]
    private void DeleteCards()
    {

    }

    [RelayCommand]
    private void ManageVendors()
    {
        var vendors = _service.GetVendors();
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Manage Vendors", _vmFactory.ManageVendors().WithData(vendors))
        });
    }

    [RelayCommand]
    private void MoveToInventory()
    {

    }

    [RelayCommand]
    private void EditItem()
    {
        Messenger.Send(new OpenDrawerMessage
        {
            DrawerWidth = 600,
            ViewModel = _vmFactory.Drawer().WithContent("Edit Wishlist Item", _vmFactory.EditWishlistItem().WithData(this.SelectedItems[0]))
        });
    }

    void IRecipient<CardsAddedToWishlistMessage>.Receive(CardsAddedToWishlistMessage message)
    {
        foreach (var item in message.Added)
        {
            this.Cards.Add(_vmFactory.WishListItem().WithData(item));
        }
    }

    private void ApplySummary()
    {
        var summary = _service.GetWishlistSpend();
        this.WishlistSummary = $"Current spend: ${summary.Total.Amount} across {summary.Vendors.Length} vendor(s)";
    }

    void IRecipient<WishlistItemUpdatedMessage>.Receive(WishlistItemUpdatedMessage message)
    {
        this.ApplySummary();
    }
}
