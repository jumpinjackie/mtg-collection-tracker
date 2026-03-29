using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class MoveWishlistItemsToCollectionViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;

    public MoveWishlistItemsToCollectionViewModel(ICollectionTrackingService service, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        this.AvailableContainers = service.GetContainers().Select(c => new ContainerViewModel().WithData(c));
    }

    public MoveWishlistItemsToCollectionViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.WishListItems =
        [
            new MoveWishlistSelectionItemViewModel
            {
                CardName = "Ancestral Recall",
                Edition = "LEA",
                AvailableQty = 2,
                QuantityToMove = 2
            }
        ];
    }

    public ObservableCollection<MoveWishlistSelectionItemViewModel> WishListItems { get; private set; } = [];

    public MoveWishlistItemsToCollectionViewModel WithData(ObservableCollection<WishlistItemViewModel> items)
    {
        this.WishListItems =
        [
            ..items.Select(w => new MoveWishlistSelectionItemViewModel
            {
                Id = w.Id,
                CardName = w.CardName,
                Edition = w.Edition,
                AvailableQty = w.QuantityNum,
                QuantityToMove = w.QuantityNum
            })
        ];
        return this;
    }

    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;

    public IEnumerable<ContainerViewModel>? AvailableContainers { get; internal set; }

    [RelayCommand]
    private void Cancel() => Messenger.Send(new CloseDialogMessage());

    [RelayCommand]
    private async Task Move()
    {
        var arg = new MoveWishlistItemsToCollectionInputModel
        {
            Items = this.WishListItems
                .Select(w => new MoveWishlistItemQuantityInputModel(
                    w.Id,
                    Math.Clamp(w.QuantityToMove, 1, w.AvailableQty)))
                .ToArray(),
            ContainerId = this.SelectedContainer?.Id
        };
        var result = await _service.MoveWishlistItemsToCollectionAsync(arg);
        Messenger.Send(new WishlistItemsAddedToCollectionMessage(result));
        Messenger.Send(new CloseDialogMessage());
    }
}

public partial class MoveWishlistSelectionItemViewModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _cardName = string.Empty;

    [ObservableProperty]
    private string _edition = string.Empty;

    [ObservableProperty]
    private int _availableQty;

    [ObservableProperty]
    private int _quantityToMove;

    partial void OnAvailableQtyChanged(int value)
    {
        if (value < 1)
        {
            this.AvailableQty = 1;
            return;
        }

        if (this.QuantityToMove > value)
        {
            this.QuantityToMove = value;
        }
    }

    partial void OnQuantityToMoveChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, this.AvailableQty);
        if (clamped != value)
        {
            this.QuantityToMove = clamped;
        }
    }
}
