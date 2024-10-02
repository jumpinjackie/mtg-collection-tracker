using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class DatabaseMaintenanceViewModel : RecipientViewModelBase, IViewModelWithBusyState
{
    readonly ICollectionTrackingService _service;

    public DatabaseMaintenanceViewModel(ICollectionTrackingService service, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
    }

    public DatabaseMaintenanceViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        _service = new StubCollectionTrackingService();
    }

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
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            this.Total = 100;
            this.Completed = 0;
            await Task.Delay(1000, cancel);
            this.Completed = 10;
            await Task.Delay(1000, cancel);
            this.Completed = 20;
            await Task.Delay(1000, cancel);
            this.Completed = 40;
            await Task.Delay(1000, cancel);
            this.Completed = 50;
            await Task.Delay(1000, cancel);
            this.Completed = 60;
            await Task.Delay(1000, cancel);
            this.Completed = 70;
            await Task.Delay(1000, cancel);
            this.Completed = 90;
            await Task.Delay(1000, cancel);
            this.Completed = 100;

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
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            this.Total = 100;
            this.Completed = 0;
            await Task.Delay(1000, cancel);
            this.Completed = 10;
            await Task.Delay(1000, cancel);
            this.Completed = 20;
            await Task.Delay(1000, cancel);
            this.Completed = 40;
            await Task.Delay(1000, cancel);
            this.Completed = 50;
            await Task.Delay(1000, cancel);
            this.Completed = 60;
            await Task.Delay(1000, cancel);
            this.Completed = 70;
            await Task.Delay(1000, cancel);
            this.Completed = 90;
            await Task.Delay(1000, cancel);
            this.Completed = 100;

            Messenger.ToastNotify("All metadata rebuilt");
        }
    }
}
