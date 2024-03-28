using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class EditCardSkuViewModel : DrawerContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    public EditCardSkuViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
    }

    public EditCardSkuViewModel(ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
    }

    public IEnumerable<int> Ids { get; private set; }

    private CardSkuItemViewModel _orig;

    public EditCardSkuViewModel WithSku(CardSkuItemViewModel sku)
    {
        _orig = sku;

        this.Ids = [sku.Id];
        this.CardName = sku.OriginalCardName;
        this.CollectorNumber = sku.CollectorNumber;
        this.Edition = sku.OriginalEdition;
        this.Language = sku.Language;
        this.Quantity = sku.OriginalEdition == "PROXY" ? sku.ProxyQty : sku.RealQty;
        this.Comments = sku.Comments;
        return this;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _cardName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _edition;

    [ObservableProperty]
    private string? _language;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int? _quantity;

    [ObservableProperty]
    private string? _comments;

    [ObservableProperty]
    private string? _collectorNumber;

    private bool CanSave() => !string.IsNullOrEmpty(CardName) && !string.IsNullOrEmpty(Edition) && Quantity > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save(CancellationToken cancel)
    {
        var m = new Core.Model.UpdateCardSkuInputModel
        {
            Ids = this.Ids
        };
        if (!string.IsNullOrEmpty(CardName))
            m.CardName = CardName;
        if (!string.IsNullOrEmpty(Edition))
            m.Edition = Edition;
        if (Quantity > 0)
            m.Quantity = Quantity;
        if (!string.IsNullOrEmpty(Language))
            m.Language = Language;
        if (!string.IsNullOrEmpty(CollectorNumber))
            m.CollectorNumber = CollectorNumber;
        if (!string.IsNullOrEmpty(Comments))
            m.Comments = Comments;

        await _service.UpdateCardSkuAsync(m, _scryfallApiClient, cancel);

        Messenger.ToastNotify("Card Sku updated");

        var newSku = await _service.GetCardSkuByIdAsync(_orig.Id, cancel);
        _orig.WithData(newSku);

        Messenger.Send(new CloseDrawerMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDrawerMessage());
    }
}
