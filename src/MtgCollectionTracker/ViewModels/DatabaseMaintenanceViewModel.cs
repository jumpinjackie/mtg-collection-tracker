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
            Messenger.ToastNotify("Adding missing metadata. Please wait ...");

            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    this.Total = total;
                }
            };
            await _service.AddMissingMetadataAsync(cb, _client, cancel);

            Messenger.ToastNotify("Missing metadata processed");
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
            Messenger.ToastNotify("Rebuilding all metadata. Please wait ...");

            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    this.Completed = processed;
                    this.Total = total;
                }
            };
            await _service.RebuildAllMetadataAsync(cb, _client, cancel);

            Messenger.ToastNotify("All metadata rebuilt");
        }
    }
}
