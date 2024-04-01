using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class AddCardsViewModel : DrawerContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;
    readonly LanguageViewModel[] _languages;

    public AddCardsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _languages = [
            new LanguageViewModel("en", "en", "English"),
            new LanguageViewModel("es", "sp", "Spanish"),
            new LanguageViewModel("fr", "fr", "French"),
            new LanguageViewModel("de", "de", "German"),
            new LanguageViewModel("ja", "jp", "Japanese")
        ];

        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Black Lotus", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Pearl", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Emerald", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Ruby", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Jet", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Mox Sapphire", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Ancestral Recall", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Time Walk", Edition = "LEB" });
        this.Cards.Add(new() { Languages = _languages, AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "Timetwister", Edition = "LEB" });
    }

    public AddCardsViewModel(IMessenger messenger, ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _languages = service.GetLanguages().Select(lang => new LanguageViewModel(lang.Code, lang.PrintedCode, lang.Name)).ToArray();
    }

    public ObservableCollection<AddCardSkuViewModel> Cards { get; } = new();

    [RelayCommand]
    private void AddRow()
    {
        Cards.Add(new AddCardSkuViewModel { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "", Edition = "", Languages = _languages });
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveCard(AddCardSkuViewModel item)
    {
        this.Cards.Remove(item);
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddCards() => Cards.Count > 0 && Cards.All(c => c.IsValid);

    [ObservableProperty]
    private int? _deckId;

    [ObservableProperty]
    private int? _containerId;

    [RelayCommand(CanExecute = nameof(CanAddCards))]
    private async Task AddCards()
    {
        var adds = this.Cards.Select(c => new AddToDeckOrContainerInputModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            IsFoil = c.IsFoil,
            Language = c.Language?.Code ?? "en",
            CollectorNumber = c.CollectorNumber,
            Quantity = c.Qty,
            Edition = c.Edition
        });
        var (total, proxyTotal, rows) = await _service.AddMultipleToContainerOrDeckAsync(this.ContainerId, this.DeckId, adds, _scryfallApiClient);
        Messenger.Send(new CardsAddedMessage { CardsTotal = total, ProxyTotal = proxyTotal, SkuTotal = rows });
        Messenger.Send(new CloseDrawerMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDrawerMessage());
    }
}
