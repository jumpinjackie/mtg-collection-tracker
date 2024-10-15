using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.Generic;
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
    }

    public int[] WishlistItemIds { get; private set; } = [];

    public MoveWishlistItemsToCollectionViewModel WithData(int[] ids)
    {
        this.WishlistItemIds = ids;
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
            WishlistItemIds = this.WishlistItemIds,
            ContainerId = this.SelectedContainer?.Id
        };
        var result = await _service.MoveWishlistItemsToCollectionAsync(arg);
        Messenger.Send(new WishlistItemsAddedToCollectionMessage(result));
        Messenger.Send(new CloseDialogMessage());
    }
}
