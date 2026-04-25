using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class DismantleDeckViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly Func<ContainerViewModel> _container;

    private Func<int?, ValueTask>? _confirmAction;

    public DismantleDeckViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _container = () => new();
        this.Message = "Are you sure you want to dismantle (My Deck)?";
        var unparented = new ContainerViewModel { Name = "(Unparented)" };
        this.AvailableContainers = [
            unparented,
            new ContainerViewModel().WithData(new() { Id = 1, Name = "Main Binder" }),
            new ContainerViewModel().WithData(new() { Id = 2, Name = "Secondary Binder" }),
            new ContainerViewModel().WithData(new() { Id = 3, Name = "Shoe Box" })
        ];
        this.SelectedContainer = unparented;
    }

    public DismantleDeckViewModel(IMessenger messenger,
                                   ICollectionTrackingService service,
                                   Func<ContainerViewModel> container)
        : base(messenger)
    {
        _service = service;
        _container = container;
    }

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _canExecuteConfirm = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _canExecuteCancel = true;

    /// <summary>
    /// The selected container. Null means (Unparented).
    /// </summary>
    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;

    public IEnumerable<ContainerViewModel>? AvailableContainers { get; private set; }

    public async Task<DismantleDeckViewModel> WithDeckAsync(int deckId, string deckName, Func<int?, ValueTask> onConfirm)
    {
        this.Message = $"Are you sure you want to dismantle ({deckName})?";
        var unparented = new ContainerViewModel { Name = "(Unparented)" };
        var containers = new List<ContainerViewModel> { unparented };
        containers.AddRange((await _service.GetContainersAsync(CancellationToken.None)).Select(c => _container().WithData(c)));
        this.AvailableContainers = containers;
        this.SelectedContainer = unparented;
        _confirmAction = onConfirm;
        return this;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteConfirm))]
    private async Task Confirm()
    {
        if (_confirmAction != null)
        {
            this.CanExecuteConfirm = false;
            await _confirmAction.Invoke(this.SelectedContainer is { Id: > 0 } c ? c.Id : (int?)null);
            this.CanExecuteConfirm = true;
            this.Messenger.Send(new CloseDialogMessage());
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCancel))]
    private void Cancel()
    {
        this.Messenger.Send(new CloseDialogMessage());
    }
}
