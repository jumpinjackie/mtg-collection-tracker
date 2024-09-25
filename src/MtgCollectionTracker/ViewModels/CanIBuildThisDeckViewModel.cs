﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public record DeckListCardItem(string CardName, int Requested, int Short, HashSet<string> FromDecks, HashSet<string> FromContainers)
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
    readonly ICollectionTrackingService _service;
    readonly IViewModelFactory _vmFactory;
    readonly IScryfallApiClient? _client;

    public CanIBuildThisDeckViewModel(ICollectionTrackingService service, IViewModelFactory vmFactory, IMessenger messenger, IScryfallApiClient client)
        : base(messenger)
    {
        _service = service;
        _vmFactory = vmFactory;
        _client = client;
    }

    public CanIBuildThisDeckViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        this.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        _vmFactory = new StubViewModelFactory();
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
        var shortOnly = _deckListCardItems.Where(d => d.Short > 0).ToList();
        if (shortOnly.Count > 0)
        {
            var wishlistItems = new List<(int qty, string cardName, string edition)>();
            var resolved = await _service.ResolveEditionsForCardsAsync(shortOnly.Select(c => c.CardName), _client!);
            foreach (var c in shortOnly)
            {
                if (resolved.TryGetValue(c.CardName, out var ed))
                    wishlistItems.Add((c.Short, ed.CardName ?? c.CardName, ed.Edition ?? string.Empty));
                else
                    wishlistItems.Add((c.Short, c.CardName, string.Empty));
            }

            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithContent("Add Cards to Wishlist", _vmFactory
                    .AddCardsToWishlist()
                    .WithCards(wishlistItems))
            });
        }
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
            var (shortAmt, fromDecks, fromContainers, suggestedName) = await _service.CheckQuantityShortfallAsync(card.CardName, card.Count, this.NoProxies, this.SparesOnly);
            if (shortAmt > 0)
                this.HasShort = true;

            if (!string.IsNullOrEmpty(suggestedName))
                _deckListCardItems.Add(new(suggestedName, card.Count, shortAmt, fromDecks, fromContainers));
            else
                _deckListCardItems.Add(new(card.CardName, card.Count, shortAmt, fromDecks, fromContainers));
        }

        var text = new StringBuilder();
        if (!this.HasShort)
        {
            text.AppendLine("Congratulations! Your collection has the cards to build this deck");
            if (!SparesOnly)
            {
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
