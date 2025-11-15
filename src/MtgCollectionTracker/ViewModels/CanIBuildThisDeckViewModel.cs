using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MtgCollectionTracker.Services;
using System.Threading;

namespace MtgCollectionTracker.ViewModels;

public record DeckListCardItem(string CardName, int Requested, int Short, HashSet<string> FromDecks, HashSet<string> FromContainers, int WishlistTotal)
{
    public bool IsShort => Short > 0;

    public string ShortTxt => Short > 0 ? Short.ToString() : string.Empty;

    // Anything more than 2 items is impractical to display on a table cell, so just provide
    // a count summary if that's the case
    public string FromDecksShort => FromDecks.Count > 2 ? $"{FromDecks.Count} different decks" : string.Join(", ", FromDecks);

    public string FromDecksFull => string.Join(Environment.NewLine, FromDecks);

    // Anything more than 2 items is impractical to display on a table cell, so just provide
    // a count summary if that's the case
    public string FromContainersShort => FromContainers.Count > 2 ? $"{FromContainers.Count} different containers" : string.Join(", ", FromContainers);

    public string FromContainersFull => string.Join(Environment.NewLine, FromContainers);
}

public partial class CanIBuildThisDeckViewModel : RecipientViewModelBase
{
    readonly IStorageProvider? _storageProvider;
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _client;
    readonly Func<DialogViewModel> _dialog;
    readonly Func<AddCardsToWishlistViewModel> _addToWishlist;
    readonly Func<LowestPriceCheckViewModel> _lowestPriceCheck;

    public CanIBuildThisDeckViewModel(IStorageProvider storageProvider,
                                      ICollectionTrackingService service,
                                      Func<DialogViewModel> dialog,
                                      Func<AddCardsToWishlistViewModel> addToWishlist,
                                      Func<LowestPriceCheckViewModel> lowestPriceCheck,
                                      IMessenger messenger,
                                      IScryfallApiClient client)
        : base(messenger)
    {
        _storageProvider = storageProvider;
        _service = service;
        _client = client;
        _dialog = dialog;
        _addToWishlist = addToWishlist;
        _lowestPriceCheck = lowestPriceCheck;
    }

    public CanIBuildThisDeckViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _dialog = () => new();
        _addToWishlist = () => new();
    }

    [ObservableProperty]
    private string _deckListContents = string.Empty;

    [ObservableProperty]
    private bool _noProxies;

    [ObservableProperty]
    private bool _ignoreSideboard;

    [ObservableProperty]
    private bool _sparesOnly;

    [ObservableProperty]
    private bool _ignoreBasicLands;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LowestPriceCheckCommand))]
    private bool _hasResult = false;

    [ObservableProperty]
    private bool _showShortDetails = false;

    [ObservableProperty]
    private string _checkResultSummary = string.Empty;

    private readonly List<DeckListCardItem> _deckListCardItems = new();

    public ObservableCollection<DeckListCardItem> DeckListReportItems { get; } = new();

    public ObservableCollection<string> FromDecks { get; } = new();

    public ObservableCollection<string> FromContainers { get; } = new();

    [ObservableProperty]
    private bool _showShortOnly;

    partial void OnShowShortOnlyChanged(bool value)
    {
        this.ApplyObservableList();
    }

    [ObservableProperty]
    private bool _hasShort;

    [RelayCommand]
    private async Task AddToWishlist()
    {
        var shortOnly = _deckListCardItems.Where(d => d.Short > 0 && d.Short > d.WishlistTotal).ToList();
        if (shortOnly.Count > 0)
        {
            var wishlistItems = new List<(int qty, string cardName, string edition)>();
            var resolved = await _service.ResolveEditionsForCardsAsync(shortOnly.Select(c => c.CardName), _client!);
            foreach (var c in shortOnly)
            {
                var deficit = c.Short - c.WishlistTotal;
                if (resolved.TryGetValue(c.CardName, out var ed))
                    wishlistItems.Add((deficit, ed.CardName ?? c.CardName, ed.Edition ?? string.Empty));
                else
                    wishlistItems.Add((deficit, c.CardName, string.Empty));
            }

            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _dialog().WithContent("Add Cards to Wishlist", 
                    _addToWishlist()
                        .WithCards(wishlistItems))
            });
        }
    }

    private bool CanPriceCheck() => this.HasResult;

    [RelayCommand(CanExecute = nameof(CanPriceCheck))]
    private async Task LowestPriceCheck(CancellationToken cancel)
    {
        var items = _deckListCardItems.Select(i => new PriceCheckItem(i.CardName, i.Requested));
        var priceList = await _service.GetLowestPricesAsync(new(items, true), _client!, cancel);
        
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 800,
            ViewModel = _dialog().WithContent("Lowest Price Check", 
                _lowestPriceCheck()
                    .WithCards(priceList))
        });
    }

    [RelayCommand]
    private void Reset()
    {
        this.DeckListContents = string.Empty;
        this.CheckResultSummary = string.Empty;
        this.DeckListReportItems.Clear();

        this.NoProxies = false;
        this.SparesOnly = false;
        this.IgnoreBasicLands = false;
        this.IgnoreSideboard = false;
        this.HasResult = false;
    }

    [RelayCommand]
    private async Task Check()
    {
        var rdr = new DecklistReader();
        Func<DecklistEntry, bool> predicate = e => true;
        if (this.IgnoreSideboard)
            predicate = e => !e.IsSideboard;
        var list = rdr.ReadDecklist(this.DeckListContents.Split(["\r\n", "\r", "\n"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Where(predicate)
            .GroupBy(ent => ent.CardName)
            .Select(grp => new { CardName = grp.Key, Count = grp.Sum(c => c.Quantity), Short = 0 })
            .ToList();

        this.CheckResultSummary = string.Empty;
        _deckListCardItems.Clear();
        this.HasShort = false;
        this.HasResult = false;
        this.ShowShortOnly = false;

        foreach (var card in list)
        {
            if (this.IgnoreBasicLands && _service.IsBasicLand(card.CardName))
                continue;

            //Stdout($"Checking availability of: {card.CardName}");
            var (shortAmt, fromDecks, fromContainers, suggestedName, wishlistAmt) = await _service.CheckQuantityShortfallAsync(card.CardName, card.Count, this.NoProxies, this.SparesOnly);
            if (shortAmt > 0)
                this.HasShort = true;

            if (!string.IsNullOrEmpty(suggestedName))
                _deckListCardItems.Add(new(suggestedName, card.Count, shortAmt, fromDecks, fromContainers, wishlistAmt));
            else
                _deckListCardItems.Add(new(card.CardName, card.Count, shortAmt, fromDecks, fromContainers, wishlistAmt));
        }

        var text = new StringBuilder();
        if (!this.HasShort)
        {
            text.AppendLine("Congratulations! Your collection has the cards to build this deck");
            if (!SparesOnly)
            {
                if (FromDecks.Count > 0)
                    text.AppendLine("Please note. You may need to dismantle one or more of your existing decks to build this one.");
                text.AppendLine("If you want to check if this is possible without dismantling any existing deck, re-run this check with 'Spares Only' checked");
            }
        }
        else
        {
            text.AppendLine("Bad news! We were short on some cards in your collection. These cards are indicated on the grid above");
            if (NoProxies)
            {
                text.AppendLine("You may be able to build this deck if you allow proxies");
            }
            if (SparesOnly)
            {
                text.AppendLine("You may be able to build this deck if you allow for cards already used in other decks. In this case, the unique list of decks to get these cards from are shown in the list above.");
            }
            text.Append("If your decklist contains split, adventure or double-faced cards, they may be marked missing if you did not specify the full name and do not already own at least one of the cards in your collection. In such cases, make sure to use the full name for both sides and use '//' instead of '/' as the separator");
        }

        this.ApplyObservableList();

        this.CheckResultSummary = text.ToString();
        this.HasResult = true;
    }

    [RelayCommand]
    private async Task Import()
    {
        if (_storageProvider == null)
            return;

        var selectedFiles = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Load decklist",
            FileTypeFilter = [new FilePickerFileType(null) { Patterns = ["*.txt"] }]
        });

        try
        {
            if (selectedFiles?.Count == 1)
            {
                var filePath = selectedFiles[0].TryGetLocalPath();
                if (filePath != null)
                {
                    using var stream = await selectedFiles[0].OpenReadAsync();
                    using var sr = new StreamReader(stream);
                    this.DeckListContents = await sr.ReadToEndAsync();
                    Messenger.ToastNotify("Decklist loaded", Avalonia.Controls.Notifications.NotificationType.Information);
                }
            }
        }
        catch (Exception ex)
        {
            Messenger.ToastNotify($"Error loading decklist: {ex.Message}", Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }

    private void ApplyObservableList()
    {
        // Update observable list
        this.DeckListReportItems.Clear();

        this.FromContainers.Clear();
        this.FromDecks.Clear();

        var decks = new HashSet<string>();
        var containers = new HashSet<string>();

        foreach (var card in _deckListCardItems)
        {
            if (this.ShowShortOnly) 
            {
                if (card.Short > 0)
                {
                    DeckListReportItems.Add(card);
                    decks.UnionWith(card.FromDecks);
                    containers.UnionWith(card.FromContainers);
                }
            }
            else
            {
                DeckListReportItems.Add(card);
                decks.UnionWith(card.FromDecks);
                containers.UnionWith(card.FromContainers);
            }
        }

        foreach (var d in decks.OrderBy(s => s))
        {
            this.FromDecks.Add(d);
        }

        foreach (var c in containers.OrderBy(s => s))
        {
            this.FromContainers.Add(c);
        }
    }
}
