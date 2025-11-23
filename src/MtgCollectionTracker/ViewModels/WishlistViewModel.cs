using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class WishlistViewModel : RecipientViewModelBase, IViewModelWithBusyState, IRecipient<CardsAddedToWishlistMessage>, IRecipient<WishlistItemUpdatedMessage>, IMultiModeCardListBehaviorHost, IRecipient<TagsAppliedMessage>, IRecipient<WishlistItemsAddedToCollectionMessage>
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    readonly Func<DialogViewModel> _dialog;
    readonly Func<EditWishlistItemViewModel> _editWishlistItem;
    readonly Func<AddCardsToWishlistViewModel> _addCardsToWishlist;
    readonly Func<WishlistItemViewModel> _wishlistItem;
    readonly Func<MoveWishlistItemsToCollectionViewModel> _moveToCollection;
    readonly Func<ManageVendorsViewModel> _manageVendors;

    public WishlistViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _dialog = () => new();
        _editWishlistItem = () => new();
        _addCardsToWishlist = () => new();
        _wishlistItem = () => new();
        _moveToCollection = () => new();
        _manageVendors = () => new();
        this.Tags = ["Foo", "Bar", "Baz"];
        this.Cards.CollectionChanged += Cards_CollectionChanged;
        this.SelectedTags.CollectionChanged += Tags_CollectionChanged;
        this.Behavior = new(this);
        this.IsActive = true;
    }

    public WishlistViewModel(ICollectionTrackingService service,
        Func<DialogViewModel> dialog,
        Func<EditWishlistItemViewModel> editWishlistItem,
        Func<AddCardsToWishlistViewModel> addCardsToWishlist,
        Func<WishlistItemViewModel> wishlistItem,
        Func<MoveWishlistItemsToCollectionViewModel> moveToCollection,
        Func<ManageVendorsViewModel> manageVendors,

    IScryfallApiClient scryfallApiClient)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _dialog = dialog;
        _editWishlistItem = editWishlistItem;
        _addCardsToWishlist = addCardsToWishlist;
        _wishlistItem = wishlistItem;
        _moveToCollection = moveToCollection;
        _manageVendors = manageVendors;
        var tags = service.GetTags();
        foreach (var t in tags)
        {
            this.Tags.Add(t);
        }
        this.Cards.CollectionChanged += Cards_CollectionChanged;
        this.SelectedTags.CollectionChanged += Tags_CollectionChanged;
        this.Behavior = new(this);
        this.IsActive = true;
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    private void Tags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.LoadWishlist();
    }

    private void Cards_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmptyCollection));
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.LoadWishlist();
        }
        base.OnActivated();
    }

    private void LoadWishlist()
    {
        this.Cards.Clear();
        var filter = new WishlistItemFilter(this.SelectedTags.Count > 0 ? this.SelectedTags : null);
        var items = _service.GetWishlistItems(filter);
        foreach (var item in items)
        {
            this.Cards.Add(_wishlistItem().WithData(item));
        }
        this.ApplySummary();
    }

    bool IViewModelWithBusyState.IsBusy
    {
        get => Behavior.IsBusy;
        set => Behavior.IsBusy = value;
    }

    public MultiModeCardListBehavior<WishlistItemViewModel> Behavior { get; }

    public ObservableCollection<string> Tags { get; } = new();

    public ObservableCollection<string> SelectedTags { get; } = new();

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
            ViewModel = _dialog().WithContent("Add Cards to Wishlist", _addCardsToWishlist())
        });
    }

    [RelayCommand]
    private void DeleteCards()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            var items = Behavior.SelectedItems.Select(item => $" - {item.QuantityNum}x {item.CardName}, {item.Edition}, {item.Language ?? "en"}");
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _dialog().WithConfirmation(
                    "Delete Wishlist Item",
                    $"Are you sure you want to delete these wishlist items?\n\n{string.Join("\n", items)}",
                    async () =>
                    {
                        int removed = 0;
                        var toRemove = Behavior.SelectedItems.ToList();
                        foreach (var item in toRemove)
                        {
                            await _service.DeleteWishlistItemAsync(item.Id);
                            Behavior.SelectedItems.Remove(item);
                            this.Cards.Remove(item);
                            removed++;
                        }

                        this.ApplySummary();
                        Messenger.ToastNotify($"{removed} wishlist items deleted", Avalonia.Controls.Notifications.NotificationType.Success);
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
            ViewModel = _dialog().WithContent("Manage Vendors", _manageVendors().WithData(vendors))
        });
    }

    [RelayCommand]
    private void MoveToCollection()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _dialog().WithContent(
                    "Move to Collection",
                    _moveToCollection()
                        .WithData(Behavior.SelectedItems))
            });
        }
    }

    [RelayCommand]
    private void EditItem()
    {
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 600,
            ViewModel = _dialog().WithContent("Edit Wishlist Item", _editWishlistItem().WithData(Behavior.SelectedItems[0]))
        });
    }

    [RelayCommand]
    private void GenerateBuyingList()
    {
        var buylist = _service.GenerateBuyingList();
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 600,
            ViewModel = _dialog().WithContent("Buying List", new BuyingListViewModel().WithText(buylist))
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
                        Messenger.ToastNotify($"Updated metadata for {processed} of {total} wishlist item(s)", Avalonia.Controls.Notifications.NotificationType.Success);
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
                    Messenger.ToastNotify($"Metadata updated for {updated} wishlist item(s)", Avalonia.Controls.Notifications.NotificationType.Success);
                }
            }
        }
    }

    void IRecipient<CardsAddedToWishlistMessage>.Receive(CardsAddedToWishlistMessage message)
    {
        foreach (var item in message.Added)
        {
            this.Cards.Add(_wishlistItem().WithData(item));
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

    void IRecipient<TagsAppliedMessage>.Receive(TagsAppliedMessage message)
    {
        this.Tags.Clear();
        foreach (var t in message.CurrentTags)
        {
            this.Tags.Add(t);
        }

        var toRemove = this.SelectedTags.Except(message.CurrentTags).ToList();

        // Remove selected tags no longer relevant
        foreach (var st in toRemove)
        {
            this.SelectedTags.Remove(st);
        }
    }

    void IRecipient<WishlistItemsAddedToCollectionMessage>.Receive(WishlistItemsAddedToCollectionMessage message)
    {
        var result = message.Result;
        var removedIds = result.CreatedSkus.Select(tuple => tuple.WishlistItemId);
        var toRemove = Behavior.SelectedItems.Where(i => removedIds.Contains(i.Id)).ToList();
        foreach (var item in toRemove)
        {
            Behavior.SelectedItems.Remove(item);
            this.Cards.Remove(item);
        }
        this.ApplySummary();
        Messenger.ToastNotify($"{result.CreatedSkus.Length} wishlist items moved to your collection", Avalonia.Controls.Notifications.NotificationType.Success);
    }
}
