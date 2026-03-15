using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class DatabaseMaintenanceViewModel : RecipientViewModelBase, IViewModelWithBusyState
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _client;

    public DatabaseMaintenanceViewModel(ICollectionTrackingService service, IScryfallApiClient client, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
        _client = client;
    }

    public DatabaseMaintenanceViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        _service = new StubCollectionTrackingService();
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    [ObservableProperty]
    private int? _total;

    [ObservableProperty]
    private int? _completed;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private void CancelUpdateMissingMetdata()
    {
        UpdateMissingMetadataCommand.Cancel();
    }

    [RelayCommand]
    private async Task UpdateMissingMetadata(CancellationToken cancel)
    {
        if (_client == null)
            return;

        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            Messenger.ToastNotify("Adding missing metadata. Please wait ...", Avalonia.Controls.Notifications.NotificationType.Information);

            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    this.Total = total;
                }
            };
            await _service.AddMissingMetadataAsync(cb, _client, cancel);

            Messenger.ToastNotify("Missing metadata processed", Avalonia.Controls.Notifications.NotificationType.Success);
        }
    }

    [RelayCommand]
    private void CancelRebuildAllMetadata()
    {
        RebuildAllMetadataCommand.Cancel();
    }

    [RelayCommand]
    private async Task RebuildAllMetadata(CancellationToken cancel)
    {
        if (_client == null)
            return;

        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            Messenger.ToastNotify("Rebuilding all metadata. Please wait ...", Avalonia.Controls.Notifications.NotificationType.Information);

            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    this.Total = total;
                }
            };
            await _service.RebuildAllMetadataAsync(cb, _client, cancel);

            Messenger.ToastNotify("All metadata rebuilt", Avalonia.Controls.Notifications.NotificationType.Success);
        }
    }

    [RelayCommand]
    private async Task NormalizeCardNames(CancellationToken cancel)
    {
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            Messenger.ToastNotify("Normalizing card names. Please wait ...", Avalonia.Controls.Notifications.NotificationType.Information);

            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    this.Total = total;
                }
            };
            await _service.NormalizeCardNamesAsync(cb, cancel);

            Messenger.ToastNotify("All card names normalized", Avalonia.Controls.Notifications.NotificationType.Success);
        }
    }

    [RelayCommand]
    private async Task ImportCardIdentifiers(CancellationToken cancel)
    {
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            Messenger.ToastNotify("Downloading and importing card identifiers. Please wait ...", Avalonia.Controls.Notifications.NotificationType.Information);
            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    if (total > 0) this.Total = total;
                }
            };
            await _service.ImportCardIdentifiersAsync(cb, cancel);
            Messenger.ToastNotify("Card identifiers imported", Avalonia.Controls.Notifications.NotificationType.Success);
        }
    }

    [RelayCommand]
    private async Task ImportPriceData(CancellationToken cancel)
    {
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            Messenger.ToastNotify("Checking for new price data. Please wait ...", Avalonia.Controls.Notifications.NotificationType.Information);
            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    if (total > 0) this.Total = total;
                }
            };
            var imported = await _service.ImportPriceDataAsync(cb, cancel);
            if (imported)
                Messenger.ToastNotify("Price data imported successfully", Avalonia.Controls.Notifications.NotificationType.Success);
            else
                Messenger.ToastNotify("Price data is already up to date", Avalonia.Controls.Notifications.NotificationType.Information);
        }
    }
}
