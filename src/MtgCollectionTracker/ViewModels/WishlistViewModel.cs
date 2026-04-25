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
    private readonly CardImageCache? _imageCache;

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
        CardImageCache imageCache,
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
        _imageCache = imageCache;
        _dialog = dialog;
        _editWishlistItem = editWishlistItem;
        _addCardsToWishlist = addCardsToWishlist;
        _wishlistItem = wishlistItem;
        _moveToCollection = moveToCollection;
        _manageVendors = manageVendors;
        this.Cards.CollectionChanged += Cards_CollectionChanged;
        this.SelectedTags.CollectionChanged += Tags_CollectionChanged;
        this.Behavior = new(this);
        this.IsActive = true;
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    private void Tags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _ = LoadWishlistAsync();
    }

    private void Cards_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmptyCollection));
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            _ = LoadInitialDataAsync();
        }
        base.OnActivated();
    }

    private async Task LoadInitialDataAsync()
    {
        if (this.Tags.Count == 0)
        {
            foreach (var t in await _service.GetTagsAsync(CancellationToken.None))
            {
                this.Tags.Add(t);
            }
        }

        await LoadWishlistAsync();
    }

    private async Task LoadWishlistAsync()
    {
        this.Cards.Clear();
        var filter = new WishlistItemFilter(this.SelectedTags.Count > 0 ? this.SelectedTags : null);
        var items = await _service.GetWishlistItemsAsync(filter, CancellationToken.None);
        foreach (var item in items)
        {
            this.Cards.Add(_wishlistItem().WithImageCache(_imageCache!).WithData(item));
        }
        await this.ApplySummaryAsync();
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
    private async Task AddCards()
    {
        var vm = await _addCardsToWishlist().InitializeAsync();
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent("Add Cards to Wishlist", vm, canClose: false)
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
                            await _service.DeleteWishlistItemAsync(item.Id, System.Threading.CancellationToken.None);
                            Behavior.SelectedItems.Remove(item);
                            this.Cards.Remove(item);
                            removed++;
                        }

                        _ = this.ApplySummaryAsync();
                        Messenger.ToastNotify($"{removed} wishlist items deleted", Avalonia.Controls.Notifications.NotificationType.Success);
                    })
            });
        }
    }

    [RelayCommand]
    private async Task ManageVendors()
    {
        var vendors = await _service.GetVendorsAsync(CancellationToken.None);
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent("Manage Vendors", _manageVendors().WithData(vendors))
        });
    }

    [RelayCommand]
    private async Task MoveToCollection()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            var vm = await _moveToCollection()
                .WithDataAsync(Behavior.SelectedItems);
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 400,
                ViewModel = _dialog().WithContent(
                    "Move to Collection",
                    vm)
            });
        }
    }

    [RelayCommand]
    private async Task EditItem()
    {
        var vm = await _editWishlistItem().WithDataAsync(Behavior.SelectedItems[0]);
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 600,
            ViewModel = _dialog().WithContent("Edit Wishlist Item", vm)
        });
    }

    [RelayCommand]
    private async Task GenerateBuyingList()
    {
        var buylist = await _service.GenerateBuyingListAsync(CancellationToken.None);
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

    private async Task ApplySummaryAsync()
    {
        var summary = await _service.GetWishlistSpendAsync(CancellationToken.None);
        this.WishlistSummary = $"Current spend: ${summary.Total.Amount} across {summary.Vendors.Length} vendor(s)";
    }

    void IRecipient<WishlistItemUpdatedMessage>.Receive(WishlistItemUpdatedMessage message)
    {
        _ = this.ApplySummaryAsync();
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
        Behavior.SelectedItems.Clear();
        _ = LoadWishlistAsync();
        Messenger.ToastNotify($"{result.CreatedSkus.Length} wishlist items moved to your collection", Avalonia.Controls.Notifications.NotificationType.Success);
    }
}
