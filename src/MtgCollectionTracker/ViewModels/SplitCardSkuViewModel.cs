using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class SplitCardSkuViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;

    public SplitCardSkuViewModel(IMessenger messenger, ICollectionTrackingService service)
        : base(messenger)
    {
        _service = service;
    }

    public SplitCardSkuViewModel()
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.CurrentQuantity = 4;
    }

    public int CardSkuId { get; set; }

    public int CurrentQuantity { get; set; }

    public int SplitMin => 1;

    public int SplitMax => CurrentQuantity - 1;

    private bool CanSplit() => SplitQuantity < CurrentQuantity && SplitQuantity > 0;

    [ObservableProperty]
    private int _splitQuantity;

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private async Task Split()
    {
        if (CanSplit())
        {
            var res = await _service.SplitCardSkuAsync(new()
            {
                CardSkuId = this.CardSkuId,
                Quantity = this.SplitQuantity
            });
            Messenger.Send(new NotificationMessage { Content = "Card SKU split" });
            Messenger.Send(new CloseDialogMessage());
        }
    }

    [RelayCommand]
    private void Cancel() => Messenger.Send(new CloseDialogMessage());
}
