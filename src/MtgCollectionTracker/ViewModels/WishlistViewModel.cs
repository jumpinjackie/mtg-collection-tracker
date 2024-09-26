using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistViewModel : RecipientViewModelBase, IViewModelWithBusyState, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>, IMultiModeCardListBehaviorHost
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    public WishlistViewModel()
    {
        this.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.Behavior = new(this);
        this.IsActive = true;
    }

    public WishlistViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
    {
        _vmFactory = vmFactory;
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        this.Behavior = new(this);
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

    bool IViewModelWithBusyState.IsBusy
    {
        get => Behavior.IsBusy;
        set => Behavior.IsBusy = value;
    }

    public MultiModeCardListBehavior<WishlistItemViewModel> Behavior { get; }

    public ObservableCollection<WishlistItemViewModel> Cards { get; } = new();

    public bool IsEmptyCollection => Cards.Count == 0;

    [ObservableProperty]
    private string _wishlistSummary;

    [RelayCommand]
    private void AddCards()
    {
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Add Cards to Wishlist", _vmFactory.AddCardsToWishlist())
        });
    }

    [RelayCommand]
    private void DeleteCards()
    {
        if (Behavior.SelectedItems.Count == 1)
        {
            var item = Behavior.SelectedItems[0];
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Delete Wishlist Item",
                    $"Are you sure you want to delete this wishlist item?",
                    async () =>
                    {
                        await _service.DeleteWishlistItemAsync(item.Id);
                        Messenger.ToastNotify($"Wishlist item ({item.CardName}, {item.Language ?? "en"}) deleted");
                        Behavior.SelectedItems.Remove(item);
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
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _vmFactory.Drawer().WithContent("Manage Vendors", _vmFactory.ManageVendors().WithData(vendors))
        });
    }

    [RelayCommand]
    private async Task MoveToCollection()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            var arg = new MoveWishlistItemsToCollectionInputModel
            {
                WishlistItemIds = Behavior.SelectedItems.Select(w => w.Id).ToArray()
            };
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Move to Collection",
                    $"Are you sure you want move these wishlist items to your collection?",
                    async () =>
                    {
                        var result = await _service.MoveWishlistItemsToCollectionAsync(arg);
                        var removedIds = result.CreatedSkus.Select(tuple => tuple.WishlistItemId);
                        var toRemove = Behavior.SelectedItems.Where(i => removedIds.Contains(i.Id)).ToList();
                        foreach (var item in toRemove)
                        {
                            Behavior.SelectedItems.Remove(item);
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
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 600,
            ViewModel = _vmFactory.Drawer().WithContent("Edit Wishlist Item", _vmFactory.EditWishlistItem().WithData(Behavior.SelectedItems[0]))
        });
    }

    [RelayCommand]
    private void GenerateBuyingList()
    {
        var buylist = _service.GenerateBuyingList();
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 600,
            ViewModel = _vmFactory.Drawer().WithContent("Buying List", new BuyingListViewModel().WithText(buylist))
        });
    }

    [RelayCommand]
    private async Task UpdateMetadata()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                int updated = 0;
                var ids = Behavior.SelectedItems.Select(c => c.Id).ToList();
                var callback = new UpdateCardMetadataProgressCallback
                {
                    OnProgress = (processed, total) =>
                    {
                        Messenger.ToastNotify($"Updated metadata for {processed} of {total} wishlist item(s)");
                    }
                };
                // FIXME: With multiple selections, it seems in general one needs to invoke this twice for the new
                // metadata to stick. I currently don't know why this is the case
                var updatedWishlist = await _service.UpdateWishlistMetadataAsync(ids, _scryfallApiClient, callback, CancellationToken.None);
                foreach (var w in updatedWishlist)
                {
                    var wishM = this.Cards.FirstOrDefault(c => c.Id == w.Id);
                    if (wishM != null)
                    {
                        wishM.WithData(w);
                        updated++;
                    }
                }
                if (updated > 0)
                {
                    Messenger.ToastNotify($"Metadata updated for {updated} wishlist item(s)");
                }
            }
        }
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

    void IMultiModeCardListBehaviorHost.HandleBusyChanged(bool oldValue, bool newValue)
    {
        
    }
}
