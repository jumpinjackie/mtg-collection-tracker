using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistViewModel : RecipientViewModelBase, IViewModelWithBusyState, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>
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
    [NotifyPropertyChangedFor(nameof(CanDoMultiSelectionCommand))]
    [NotifyPropertyChangedFor(nameof(CanDoSingleSelectionCommand))]
    private bool _isBusy;

    public bool IsEmptyCollection => Cards.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDoMultiSelectionCommand))]
    private bool _hasSelectedItems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDoSingleSelectionCommand))]
    private bool _hasSingleSelectedItem;

    [ObservableProperty]
    private string _wishlistSummary;

    public bool CanDoSingleSelectionCommand => HasSingleSelectedItem && !IsBusy;

    public bool CanDoMultiSelectionCommand => HasSelectedItems && !IsBusy;

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
        if (this.SelectedItems.Count == 1)
        {
            var item = this.SelectedItems[0];
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Delete Wishlist Item",
                    $"Are you sure you want to delete this wishlist item?",
                    async () =>
                    {
                        await _service.DeleteWishlistItemAsync(item.Id);
                        Messenger.ToastNotify($"Wishlist item ({item.CardName}, {item.Language ?? "en"}) deleted");
                        this.SelectedItems.Remove(item);
                        this.Cards.Remove(item);
                        this.ApplySummary();
                    })
            });
        }
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
    private async Task MoveToCollection()
    {
        if (this.SelectedItems.Count > 0)
        {
            var arg = new MoveWishlistItemsToCollectionInputModel
            {
                WishlistItemIds = this.SelectedItems.Select(w => w.Id).ToArray()
            };
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Move to Collection",
                    $"Are you sure you want move these wishlist items to your collection?",
                    async () =>
                    {
                        var result = await _service.MoveWishlistItemsToCollectionAsync(arg);
                        var removedIds = result.CreatedSkus.Select(tuple => tuple.WishlistItemId);
                        var toRemove = this.SelectedItems.Where(i => removedIds.Contains(i.Id)).ToList();
                        foreach (var item in toRemove)
                        {
                            this.SelectedItems.Remove(item);
                            this.Cards.Remove(item);
                        }
                        this.ApplySummary();
                        Messenger.ToastNotify($"{result.CreatedSkus.Length} wishlist items moved to your collection");
                    })
            });
        }
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
