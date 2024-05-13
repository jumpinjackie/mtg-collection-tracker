using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
        this.AvailableDecks = [
            new DeckViewModel().WithData(new() { Id = 1, Name = "My Vintage Deck"}),
            new DeckViewModel().WithData(new() { Id = 2, Name = "My Legacy Deck"}),
        ];
        this.AvailableContainers = [
            new ContainerViewModel().WithData(new()  { Id = 1, Name = "My Binder"}),
            new ContainerViewModel().WithData(new()  { Id = 2, Name = "My Shoebox"})
        ];
    }

    public EditCardSkuViewModel(ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        this.Languages = service.GetLanguages().Select(lang => new LanguageViewModel(lang.Code, lang.PrintedCode, lang.Name)).ToArray();
        this.AvailableDecks = service.GetDecks(null).Select(deck => new DeckViewModel().WithData(deck));
        this.AvailableContainers = service.GetContainers().Select(cnt => new ContainerViewModel().WithData(cnt));
    }

    public IEnumerable<DeckViewModel> AvailableDecks { get; private set; }

    public IEnumerable<ContainerViewModel> AvailableContainers { get; private set; }

    [ObservableProperty]
    private DeckViewModel? _deck;

    [ObservableProperty]
    private ContainerViewModel? _container;

    public IEnumerable<int> Ids { get; private set; }

    private CardSkuItemViewModel _origItem;

    public EditCardSkuViewModel WithSku(CardSkuItemViewModel sku)
    {
        _origItem = sku;

        this.Ids = [sku.Id];
        this.CardName = sku.OriginalCardName;
        this.CollectorNumber = sku.CollectorNumber;
        this.Edition = sku.OriginalEdition;
        this.Language = this.Languages.FirstOrDefault(lang => lang.Code == sku.Language);
        this.Quantity = sku.OriginalEdition == "PROXY" ? sku.ProxyQty : sku.RealQty;
        this.Comments = sku.Comments;
        return this;
    }

    private IEnumerable<CardSkuItemViewModel> _origItems;

    public EditCardSkuViewModel WithSkus(IEnumerable<CardSkuItemViewModel> skus)
    {
        _origItems = skus.ToList();
        this.Ids = skus.Select(s => s.Id).ToList();

        var uniqCardNames = new HashSet<string?>();
        var uniqCollectors = new HashSet<string?>();
        var uniqEditions = new HashSet<string?>();
        var uniqLanguages = new HashSet<string?>();
        var uniqComments = new HashSet<string?>();

        foreach (var sku in _origItems)
        {
            uniqCardNames.Add(sku.OriginalCardName);
            uniqCollectors.Add(sku.CollectorNumber);
            uniqEditions.Add(sku.Edition);
            uniqLanguages.Add(sku.Language);
            uniqComments.Add(sku.Comments);
        }

        this.CardName = uniqCardNames.Count == 1 ? uniqCardNames.First() : null;
        this.CollectorNumber = uniqCollectors.Count == 1 ? uniqCollectors.First() : null;
        this.Edition = uniqEditions.Count == 1 ? uniqEditions.First() : null;
        this.Language = uniqLanguages.Count == 1 ? this.Languages.FirstOrDefault(lang => lang.Code == uniqLanguages.First()) : null;
        
        this.Comments = uniqComments.Count == 1 ? uniqComments.First() : null;
        return this;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyCardName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyQuantity;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyEdition;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyLanguage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyCollector;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyComments;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyDeck;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyContainer;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool? _isLand;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool? _isSideboard;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool? _unsetDeck;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool? _unsetContainer;

    private bool CanSave()
    {
        return this.ApplyCardName
            || this.ApplyCollector
            || this.ApplyComments
            || this.ApplyContainer
            || this.ApplyDeck
            || this.ApplyEdition
            || this.ApplyLanguage
            || this.ApplyQuantity
            || this.IsLand.HasValue
            || this.IsSideboard.HasValue
            || this.UnsetContainer.HasValue
            || this.UnsetDeck.HasValue;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save(CancellationToken cancel)
    {
        var m = new Core.Model.UpdateCardSkuInputModel
        {
            Ids = this.Ids
        };
        if (!string.IsNullOrEmpty(CardName) && ApplyCardName)
            m.CardName = CardName;
        if (!string.IsNullOrEmpty(Edition) && ApplyEdition)
            m.Edition = Edition;
        if (Quantity > 0 && ApplyQuantity)
            m.Quantity = Quantity;
        if (Language != null && ApplyLanguage)
            m.Language = Language.Code;
        if (!string.IsNullOrEmpty(CollectorNumber) && ApplyCollector)
            m.CollectorNumber = CollectorNumber;
        if (!string.IsNullOrEmpty(Comments) && ApplyComments)
            m.Comments = Comments;
        if (Deck != null && ApplyDeck)
            m.DeckId = Deck.DeckId;
        if (Container != null && ApplyContainer)
            m.ContainerId = Container.Id;

        m.IsLand = IsLand;
        m.IsSideboard = IsSideboard;
        if (UnsetContainer.HasValue)
            m.UnsetContainer = UnsetContainer.Value;
        if (UnsetDeck.HasValue)
            m.UnsetDeck = UnsetDeck.Value;

        await _service.UpdateCardSkuAsync(m, _scryfallApiClient, cancel);

        Messenger.ToastNotify("Card Sku(s) updated");

        if (_origItem != null)
        {
            var newSku = await _service.GetCardSkuByIdAsync(_origItem.Id, cancel);
            _origItem.WithData(newSku);
        }
        else if (_origItems != null)
        {
            foreach (var sku in _origItems)
            {
                var newSku = await _service.GetCardSkuByIdAsync(sku.Id, cancel);
                sku.WithData(newSku);
            }
        }

        Messenger.Send(new CloseDrawerMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDrawerMessage());
    }
}
