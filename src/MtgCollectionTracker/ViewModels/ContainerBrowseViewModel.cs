﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
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

public partial class ContainerBrowseViewModel : DialogContentViewModel, IViewModelWithBusyState, IMultiModeCardListBehaviorHost, IRecipient<CardsSentToDeckMessage>, IRecipient<CardSkuSplitMessage>, IRecipient<CardsSentToContainerMessage>, IRecipient<CardsOrphanedMessage>
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    readonly Func<CardSkuItemViewModel> _cardSku;
    readonly Func<DialogViewModel> _dialog;
    readonly Func<EditCardSkuViewModel> _editCardSku;
    readonly Func<AddCardsViewModel> _addCards;
    readonly Func<SendCardsToContainerOrDeckViewModel> _sendToContainer;
    readonly Func<SplitCardSkuViewModel> _splitCardSku;

    public ContainerBrowseViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _addCards = () => new();
        _cardSku = () => new();
        _dialog = () => new();
        _editCardSku = () => new();
        _sendToContainer = () => new();
        _splitCardSku = () => new();
        this.Behavior = new(this);
        this.IsActive = true;
    }

    public ContainerBrowseViewModel(ICollectionTrackingService service,
                                    Func<CardSkuItemViewModel> cardSku,
                                    Func<DialogViewModel> dialog,
                                    Func<EditCardSkuViewModel> editCardSku,
                                    Func<AddCardsViewModel> addCards,
                                    Func<SendCardsToContainerOrDeckViewModel> sendToContainer,
                                    Func<SplitCardSkuViewModel> splitCardSku,
                                    IScryfallApiClient scryfallApiClient,
                                    IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _addCards = addCards;
        _cardSku = cardSku;
        _dialog = dialog;
        _editCardSku = editCardSku;
        _sendToContainer = sendToContainer;
        _splitCardSku = splitCardSku;
        this.Behavior = new(this);
        this.IsActive = true;
    }

    private int? _containerId;

    [ObservableProperty]
    private int _pageNumber = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviousEnabled))]
    private bool _canGoPrevious;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NextEnabled))]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _showOnlyMissingMetadata;

    partial void OnShowOnlyMissingMetadataChanged(bool value)
    {
        FetchPage(this.PageNumber);
    }

    public MultiModeCardListBehavior<CardSkuItemViewModel> Behavior { get; }

    private void FetchPage(int oneBasedPageNumber)
    {
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            var page = _service.GetCardsForContainer(_containerId.Value, new()
            {
                ShowOnlyMissingMetadata = this.ShowOnlyMissingMetadata,
                // TODO: Dynamically compute desired page size based on screen real estate
                // right now it is hard-coded to 16
                PageSize = 16,
                PageNumber = oneBasedPageNumber - 1
            });
            Behavior.SelectedItems.Clear();
            Behavior.SelectedRow = null;
            this.CurrentPage.Clear();
            foreach (var sku in page.Items)
            {
                this.CurrentPage.Add(_cardSku().WithData(sku));
            }
            this.HasNoResults = this.CurrentPage.Count == 0;
            var from = Math.Max(page.PageNumber, 0) * page.PageSize;
            var to = Math.Min((page.PageNumber + 1) * page.PageSize, page.Total);
            this.PageSummary = $"Viewing {from} - {to} of {page.Total} skus";
            this.CanGoPrevious = (oneBasedPageNumber - 1) > 0;
            this.CanGoNext = (this.CurrentPage.Count == page.PageSize);
        }
    }

    partial void OnPageNumberChanged(int oldValue, int newValue)
    {
        if (oldValue != newValue && newValue > 0 && _containerId.HasValue)
        {
            FetchPage(newValue);
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        this.PageNumber++;
    }

    [RelayCommand]
    private void PreviousPage()
    {
        this.PageNumber--;
    }

    [RelayCommand]
    private void EditSelectedSku()
    {
        if (Behavior.SelectedItems.Count == 1)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 600,
                ViewModel = _dialog().WithContent("Edit Sku", _editCardSku().WithSku(Behavior.SelectedItems[0]))
            });
        }
        else if (Behavior.SelectedItems.Count > 1)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 600,
                ViewModel = _dialog().WithContent("Edit Skus", _editCardSku().WithSkus(Behavior.SelectedItems))
            });
        }
    }

    public ContainerBrowseViewModel WithContainerId(int containerId)
    {
        _containerId = containerId;
        this.PageNumber = 1;
        return this;
    }

    public bool NextEnabled => this.CanGoNext && !Behavior.IsBusy;

    public bool PreviousEnabled => this.CanGoPrevious && !Behavior.IsBusy;

    
    [ObservableProperty]
    private string? _containerSummary;

    [ObservableProperty]
    private string? _pageSummary;

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private bool _hasNoResults;

    public ObservableCollection<CardSkuItemViewModel> CurrentPage { get; } = new();

    [RelayCommand]
    private void AddSkus()
    {
        var vm = _addCards();
        if (_containerId.HasValue)
            vm = vm.WithTargetContainer(_containerId.Value);

        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent("Add Cards", vm)
        });
    }

    [RelayCommand]
    private void ViewSelectedSku()
    {
        Messenger.ToastNotify("Feature not implemented yet", Avalonia.Controls.Notifications.NotificationType.Error);
    }

    [RelayCommand]
    private void SendSkusToContainer()
    {
        if (Behavior.SelectedItems.Count > 0)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _dialog().WithContent("Send Cards To Deck or Container", _sendToContainer().WithCards(Behavior.SelectedItems))
            });
        }
    }

    [RelayCommand]
    private async Task UpdateSkuMetadata()
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
                        Messenger.ToastNotify($"Updated metadata for {processed} of {total} sku(s)", Avalonia.Controls.Notifications.NotificationType.Success);
                    }
                };
                // FIXME: With multiple selections, it seems in general one needs to invoke this twice for the new
                // metadata to stick. I currently don't know why this is the case
                var updatedSkus = await _service.UpdateCardMetadataAsync(ids, _scryfallApiClient, callback, CancellationToken.None);
                foreach (var sku in updatedSkus)
                {
                    var skuM = this.CurrentPage.FirstOrDefault(c => c.Id == sku.Id);
                    if (skuM != null)
                    {
                        skuM.WithData(sku);
                        updated++;
                    }
                }
                if (updated > 0)
                {
                    Messenger.ToastNotify($"Metadata updated for {updated} sku(s)", Avalonia.Controls.Notifications.NotificationType.Success);
                }
            }
        }
    }

    [RelayCommand]
    private void SplitSelectedSku()
    {
        if (Behavior.IsItemSplittable)
        {
            var selected = Behavior.SelectedItems[0];
            var vm = _splitCardSku();
            vm.CardSkuId = selected.Id;
            if (selected.ProxyQty > 1)
            {
                vm.CurrentQuantity = selected.ProxyQty;
            }
            else if (selected.RealQty > 1)
            {
                vm.CurrentQuantity = selected.RealQty;
            }
            if (vm.CurrentQuantity == 0)
                return;

            vm.SplitQuantity = vm.SplitMin;
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 300,
                ViewModel = _dialog().WithContent("Split Card SKU", vm)
            });
        }
    }

    bool IViewModelWithBusyState.IsBusy
    {
        get => Behavior.IsBusy;
        set => Behavior.IsBusy = value;
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    void IMultiModeCardListBehaviorHost.HandleBusyChanged(bool oldValue, bool newValue)
    {
        this.OnPropertyChanged(nameof(PreviousEnabled));
        this.OnPropertyChanged(nameof(NextEnabled));
    }

    void IRecipient<CardsSentToDeckMessage>.Receive(CardsSentToDeckMessage message)
    {
        // This item was moved out of this container, so refresh current page
        if (this.PageNumber > 0)
            FetchPage(this.PageNumber);
    }

    void IRecipient<CardSkuSplitMessage>.Receive(CardSkuSplitMessage message)
    {
        var toUpdate = this.CurrentPage
                .Where(r => r.Id == message.SplitSkuId)
                .Select(r => r.Id)
                .ToList();
        var updatedSkus = _service.GetCards(new() { CardSkuIds = toUpdate });
        foreach (var sku in updatedSkus)
        {
            var item = this.CurrentPage.FirstOrDefault(r => r.Id == sku.Id);
            if (item != null)
            {
                item.WithData(sku);
                var idx = this.CurrentPage.IndexOf(item);
                // Add the new split sku as well
                var newSku = _service.GetCards(new() { CardSkuIds = [message.NewSkuId] }).ToList();
                if (newSku.Count == 1)
                {
                    this.CurrentPage.Insert(idx, _cardSku().WithData(newSku[0]));
                }
            }
        }
    }

    void IRecipient<CardsSentToContainerMessage>.Receive(CardsSentToContainerMessage message)
    {
        // Remove from current page any skus no longer in this container
        if (message.ContainerId != _containerId)
        {
            var toRemove = this.CurrentPage
               .Where(r => message.SkuIds.Contains(r.Id))
               .ToList();

            foreach (var remove in toRemove)
            {
                this.CurrentPage.Remove(remove);
            }
        }
    }

    void IRecipient<CardsOrphanedMessage>.Receive(CardsOrphanedMessage message)
    {
        // Remove from current page any skus no longer in this container
        var toRemove = this.CurrentPage
               .Where(r => message.SkuIds.Contains(r.Id))
               .ToList();

        foreach (var remove in toRemove)
        {
            this.CurrentPage.Remove(remove);
        }
    }
}