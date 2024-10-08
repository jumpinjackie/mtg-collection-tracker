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

public partial class AddCardsToWishlistViewModel : DialogContentViewModel
{
    readonly IStorageProvider _storage;
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;
    readonly LanguageViewModel[] _languages;

    public AddCardsToWishlistViewModel()
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

    public AddCardsToWishlistViewModel(IStorageProvider storage, IMessenger messenger, ICollectionTrackingService service, IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _storage = storage;
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        _languages = service.GetLanguages().Select(lang => new LanguageViewModel(lang.Code, lang.PrintedCode, lang.Name)).ToArray();
    }

    public AddCardsToWishlistViewModel WithCards(IEnumerable<(int qty, string cardName, string edition)> cards)
    {
        foreach (var c in cards)
        {
            this.Cards.Add(new AddCardSkuViewModel
            {
                Languages = _languages,
                AddCardsCommand = this.AddCardsCommand,
                Qty = c.qty,
                CardName = c.cardName,
                Edition = c.edition
            });
        }
        return this;
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
    private bool _isImporting;

    private bool CanImport => !IsImporting;

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task Import()
    {
        if (_storage == null)
            return;

        var selectedFiles = await _storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Load rows from CSV",
            FileTypeFilter = [new FilePickerFileType(null) { Patterns = ["*.csv"] }]
        });

        try
        {
            this.IsImporting = true;
            if (selectedFiles?.Count == 1)
            {
                var csvPath = selectedFiles[0].TryGetLocalPath();
                if (csvPath != null)
                {
                    var csvConf = new CsvConfiguration(CultureInfo.InvariantCulture);
                    using var sr = new StreamReader(csvPath);
                    using var csvr = new CsvReader(sr, csvConf);

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
                    {
                        Cards.Add(inr);
                    }
                }
            }
        }
        finally
        {
            this.IsImporting = false;
            AddCardsCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAddRow => !IsImporting;

    [RelayCommand(CanExecute = nameof(CanAddRow))]
    private void AddRow()
    {
        Cards.Add(new AddCardSkuViewModel { AddCardsCommand = this.AddCardsCommand, Qty = 1, CardName = "", Edition = "", Languages = _languages });
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    private bool CanRemoveCard => !IsImporting;

    [RelayCommand(CanExecute = nameof(CanRemoveCard))]
    private void RemoveCard(AddCardSkuViewModel item)
    {
        this.Cards.Remove(item);
        AddCardsCommand.NotifyCanExecuteChanged();
    }

    private bool CanAddCards() => !IsImporting && Cards.Count > 0 && Cards.All(c => c.IsValid);

    [RelayCommand(CanExecute = nameof(CanAddCards))]
    private async Task AddCards()
    {
        var adds = this.Cards.Select(c => new AddToWishlistInputModel
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

        var added = await _service.AddMultipleToWishlistAsync(adds, _scryfallApiClient);
        Messenger.Send(new CardsAddedToWishlistMessage { Added = added });
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private async Task CheckCardNames()
    {
        if (_scryfallApiClient != null)
        {
            int cardsFixed = 0;
            int editionsFixed = 0;
            foreach (var sku in this.Cards)
            {
                var (res, correctEdition, _) = await _scryfallApiClient.CheckCardNameAsync(sku.CardName, sku.Edition);
                if (res != null && sku.CardName != res)
                {
                    sku.CardName = res;
                    cardsFixed++;
                }
                if (correctEdition != null && sku.Edition.ToLower() != correctEdition.ToLower())
                {
                    sku.Edition = correctEdition.ToUpper();
                    editionsFixed++;
                }
            }
            Messenger.ToastNotify($"{cardsFixed} card name(s) and {editionsFixed} edition(s) fixed up");
        }
    }
}
