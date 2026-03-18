using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CsvHelper;
using CsvHelper.Configuration;
using MtgCollectionTracker.Core;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

record CsvImportRecord(int Qty, string CardName, string Edition, string? Language, bool? IsFoil, bool? IsLand, bool? IsSideboard, string? Condition, string? Comments);

public partial class AddCardsViewModel : DialogContentViewModel
{
    readonly IStorageProvider? _storage;
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

    public AddCardsViewModel(IStorageProvider storage, IMessenger messenger, ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _storage = storage;
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _languages = service.GetLanguages().Select(lang => new LanguageViewModel(lang.Code, lang.PrintedCode, lang.Name)).ToArray();

        this.AvailableContainers = service.GetContainers().Select(c => new ContainerViewModel().WithData(c));
    }

    public ObservableCollection<AddCardSkuViewModel> Cards { get; } = new();

    static CardCondition? TryParseCondition(string? condition)
    {
        switch (condition?.ToLower())
        {
            case "nm":
                return CardCondition.NearMint;
            case "lp":
                return CardCondition.LightlyPlayed;
            case "mp":
                return CardCondition.ModeratelyPlayed;
            case "hp":
                return CardCondition.HeavilyPlayed;
        }
        return null;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCardCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckCardNamesCommand))]
    private bool _isImporting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCardCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckCardNamesCommand))]
    private bool _isAddingCards;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCardCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckCardNamesCommand))]
    private bool _isCheckingCardNames;

    public bool IsDialogBusy => IsAddingCards || IsCheckingCardNames;

    partial void OnIsAddingCardsChanged(bool value) => OnPropertyChanged(nameof(IsDialogBusy));

    partial void OnIsCheckingCardNamesChanged(bool value) => OnPropertyChanged(nameof(IsDialogBusy));

    private bool CanImport => !IsImporting && !IsDialogBusy;

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task Import()
    {
        if (_storage == null)
            return;

        var selectedFiles = await _storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Load rows from CSV",
            FileTypeFilter = [new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] }]
        });

        try
        {
            this.IsImporting = true;
            if (selectedFiles?.Count == 1)
            {
                var stream = await selectedFiles[0].OpenReadAsync();
                var csvConf = new CsvConfiguration(CultureInfo.InvariantCulture);
                //csvConf.HeaderValidated = args => { };
                csvConf.MissingFieldFound = null;
                using var sr = new StreamReader(stream);
                using var csvr = new CsvReader(sr, csvConf);
                csvr.Read();
                csvr.ReadHeader();
                /*
                var input = csvr.GetRecords<CsvImportRecord>()
                    .Select(c => new AddCardSkuViewModel
                    {
                        AddCardsCommand = this.AddCardsCommand,
                        Languages = _languages,
                        Qty = c.Qty,
                        CardName = c.CardName,
                        Edition = c.Edition,
                        Language = _languages.FirstOrDefault(l => l.Code == c.Language || l.PrintedCode == c.Language),
                        //IsSideboard = c.IsSideboard ?? false,
                        IsFoil = c.IsFoil ?? false,
                        //IsLand = c.IsLand ?? false,
                        Condition = TryParseCondition(c.Condition),
                        Comments = c.Comments
                    });

                foreach (var inr in input)
                */
                while (csvr.Read())
                {
                    var lang = csvr.GetField(nameof(CsvImportRecord.Language));
                    var inr = new AddCardSkuViewModel
                    {
                        AddCardsCommand = this.AddCardsCommand,
                        Languages = _languages,
                        Qty = csvr.GetField<int>(nameof(CsvImportRecord.Qty)),
                        CardName = csvr.GetField(nameof(CsvImportRecord.CardName)),
                        Edition = csvr.GetField(nameof(CsvImportRecord.Edition)),
                        Language = lang != null ? _languages.FirstOrDefault(l => l.Code == lang || l.PrintedCode == lang) : null,
                        //IsSideboard = c.IsSideboard ?? false,
                        IsFoil = bool.TryParse(csvr.GetField(nameof(CsvImportRecord.IsFoil)), out var b) ? b : false,
                        //IsLand = c.IsLand ?? false,
                        Condition = TryParseCondition(csvr.GetField(nameof(CsvImportRecord.Condition))),
                        Comments = csvr.GetField(nameof(CsvImportRecord.Comments))
                    };

                    Cards.Add(inr);
                }
            }
        }
        finally
        {
            this.IsImporting = false;
            AddCardsCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAddRow => !IsImporting && !IsDialogBusy;

    [RelayCommand(CanExecute = nameof(CanAddRow))]
    private void AddRow()
    {
        Cards.Add(new AddCardSkuViewModel { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "", Edition = "", Languages = _languages });
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    private bool CanRemoveCard => !IsImporting && !IsDialogBusy;

    [RelayCommand(CanExecute = nameof(CanRemoveCard))]
    private void RemoveCard(AddCardSkuViewModel item)
    {
        this.Cards.Remove(item);
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddCards() => !IsImporting && !IsDialogBusy && Cards.Count > 0 && Cards.All(c => c.IsValid);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private ContainerViewModel? _selectedContainer;

    public IEnumerable<ContainerViewModel>? AvailableContainers { get; internal set; }

    private int? _targetDeckId;
    private string? _targetDeckName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowContainerSelector))]
    [NotifyPropertyChangedFor(nameof(ShowAddToSideboardOption))]
    private bool _lockedTargetDeck = false;

    public string? TargetDeckName => _targetDeckName;

    public bool ShowContainerSelector => !LockedTargetContainer && !LockedTargetDeck;

    public bool ShowAddToSideboardOption => LockedTargetDeck;

    [ObservableProperty]
    private bool _addToSideboard;

    [RelayCommand(CanExecute = nameof(CanAddCards))]
    private async Task AddCards()
    {
        IsAddingCards = true;
        try
        {
        int? containerId = null;
        int? deckId = null;

        if (this.SelectedContainer != null)
        {
            containerId = this.SelectedContainer.Id;
        }

        if (_targetDeckId.HasValue)
        {
            deckId = _targetDeckId.Value;
        }

        var addToSideboard = deckId.HasValue && this.AddToSideboard;

        var adds = this.Cards.Select(c => new AddToDeckOrContainerInputModel
        {
            CardName = c.CardName,
            Comments = c.Comments,
            Condition = c.Condition,
            IsFoil = c.IsFoil,
            Language = c.Language?.Code ?? "en",
            CollectorNumber = c.CollectorNumber,
            Quantity = c.Qty,
            Edition = c.Edition,
            IsSideboard = addToSideboard
        });

        var (total, proxyTotal, rows) = await _service.AddMultipleToContainerOrDeckAsync(containerId, deckId, adds, _scryfallApiClient);
        Messenger.Send(new CardsAddedMessage { CardsTotal = total, ProxyTotal = proxyTotal, SkuTotal = rows });
        if (deckId.HasValue)
        {
            Messenger.Send(new DeckTotalsChangedMessage([deckId.Value]));
            Messenger.Send(new CardsAddedToDeckMessage(deckId.Value));
        }
        Messenger.Send(new CloseDialogMessage());
        }
        finally
        {
            IsAddingCards = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowContainerSelector))]
    private bool _lockedTargetContainer = false;

    public AddCardsViewModel WithTargetContainer(int containerId)
    {
        this.SelectedContainer = this.AvailableContainers?.FirstOrDefault(c => c.Id == containerId);
        if (this.SelectedContainer != null)
            this.LockedTargetContainer = true;
        return this;
    }

    public AddCardsViewModel WithTargetDeck(int deckId, string deckName)
    {
        _targetDeckId = deckId;
        _targetDeckName = deckName;
        this.LockedTargetDeck = true;
        return this;
    }

    private bool CanCheckCardNames() => _scryfallApiClient != null && !IsImporting && !IsDialogBusy;

    [RelayCommand(CanExecute = nameof(CanCheckCardNames))]
    private async Task CheckCardNames()
    {
        if (_scryfallApiClient == null)
            return;

        IsCheckingCardNames = true;
        try
        {
            int cardsFixed = 0;
            int editionsFixed = 0;
            foreach (var sku in this.Cards)
            {
                var (found, res, correctEdition, _) = await _scryfallApiClient.CheckCardNameAsync(sku.CardName, sku.Edition);
                if (!found)
                {
                    Messenger.ToastNotify($"No such card: {sku.CardName}", Avalonia.Controls.Notifications.NotificationType.Success);
                }
                if (res != null && sku.CardName != res)
                {
                    sku.CardName = res;
                    cardsFixed++;
                }
                // Only apply correct edition if not proxy
                if (correctEdition != null && sku.Edition?.ToLower() != "proxy" && sku.Edition?.ToLower() != correctEdition.ToLower())
                {
                    sku.Edition = correctEdition.ToUpper();
                    editionsFixed++;
                }
            }
            Messenger.ToastNotify($"{cardsFixed} card name(s) and {editionsFixed} edition(s) fixed up", Avalonia.Controls.Notifications.NotificationType.Success);
        }
        finally
        {
            IsCheckingCardNames = false;
        }
    }
}
