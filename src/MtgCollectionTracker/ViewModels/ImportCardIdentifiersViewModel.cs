using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class ImportCardIdentifiersViewModel : DialogContentViewModel, IViewModelWithBusyState
{
    readonly ICollectionTrackingService _service;

    public ImportCardIdentifiersViewModel()
        : base()
    {
        _service = new MtgCollectionTracker.Services.Stubs.StubCollectionTrackingService();
    }

    public ImportCardIdentifiersViewModel(ICollectionTrackingService service, IMessenger messenger)
        : base(messenger)
    {
        _service = service;
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _completed;

    [ObservableProperty]
    private string _statusMessage = "Card identifier data is required to support price lookups. Click Begin to download and import this data.";

    [ObservableProperty]
    private bool _isDone;

    [RelayCommand]
    private async Task Begin(CancellationToken cancel)
    {
        using (((IViewModelWithBusyState)this).StartBusyState())
        {
            StatusMessage = "Downloading and importing card identifiers. Please wait...";
            var cb = new UpdateCardMetadataProgressCallback
            {
                OnProgress = (processed, total) =>
                {
                    Completed = processed;
                }
            };
            await _service.ImportCardIdentifiersAsync(cb, cancel);
            StatusMessage = $"Done! Imported {Completed} card identifiers.";
            IsDone = true;
        }
    }

    [RelayCommand]
    private void Done()
    {
        Messenger.Send(new CloseDialogMessage());
    }
}
