﻿using Avalonia.Platform.Storage;
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCardsCommand))]
    private ContainerViewModel? _selectedContainer;

    public IEnumerable<ContainerViewModel>? AvailableContainers { get; internal set; }

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

        int? containerId = null;
        int? deckId = null;

        if (this.SelectedContainer != null)
        {
            containerId = this.SelectedContainer.Id;
        }

        var (total, proxyTotal, rows) = await _service.AddMultipleToContainerOrDeckAsync(containerId, deckId, adds, _scryfallApiClient);
        Messenger.Send(new CardsAddedMessage { CardsTotal = total, ProxyTotal = proxyTotal, SkuTotal = rows });
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    [ObservableProperty]
    private bool _lockedTargetContainer = false;

    public AddCardsViewModel WithTargetContainer(int containerId)
    {
        this.SelectedContainer = this.AvailableContainers?.FirstOrDefault(c => c.Id == containerId);
        if (this.SelectedContainer != null)
            this.LockedTargetContainer = true;
        return this;
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
    }
}
