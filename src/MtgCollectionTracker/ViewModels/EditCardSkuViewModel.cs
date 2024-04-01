using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class EditCardSkuViewModel : DrawerContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;
    public LanguageViewModel[] Languages { get; }

    public EditCardSkuViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.Languages = [
            new LanguageViewModel("en", "en", "English"),
            new LanguageViewModel("es", "sp", "Spanish"),
            new LanguageViewModel("fr", "fr", "French"),
            new LanguageViewModel("de", "de", "German"),
            new LanguageViewModel("ja", "jp", "Japanese")
        ];
    }

    public EditCardSkuViewModel(ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        this.Languages = service.GetLanguages().Select(lang => new LanguageViewModel(lang.Code, lang.PrintedCode, lang.Name)).ToArray();
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
        this.Language = this.Languages.FirstOrDefault(lang => lang.Code == sku.Language);
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
    private LanguageViewModel? _language;

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
        if (Language != null)
            m.Language = Language.Code;
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
