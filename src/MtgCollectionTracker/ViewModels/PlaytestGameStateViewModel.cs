using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// ViewModel managing the playtesting game state including all zones, counters, and phase tracking
/// </summary>
public partial class PlaytestGameStateViewModel : ViewModelBase
{
    private readonly IMessenger _messenger;
    private readonly Func<PlaytestCardViewModel> _cardVmFactory;
    private readonly Random _random = new();

    public PlaytestGameStateViewModel(IMessenger messenger, Func<PlaytestCardViewModel> cardVmFactory)
    {
        _messenger = messenger;
        _cardVmFactory = cardVmFactory;
        WireCollectionObservers();
    }

    public PlaytestGameStateViewModel()
    {
        ThrowIfNotDesignMode();
        _messenger = null!;
        _cardVmFactory = null!;
        WireCollectionObservers();
    }

    // Zones
    public ObservableCollection<PlaytestCardViewModel> Library { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> Hand { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> Graveyard { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> Exile { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> Stack { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> BattlefieldLands { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> BattlefieldNonlands { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> CommandZone { get; } = new();
    public ObservableCollection<PlaytestCardViewModel> Sideboard { get; } = new();

    // Original sideboard cards for reset (not a zone, just stored for reset)
    private readonly List<PlaytestCard> _originalSideboardCards = new();

    // Multi-selection: cards on the battlefield currently selected
    public ObservableCollection<PlaytestCardViewModel> SelectedBattlefieldCards { get; } = new();

    // Counters
    [ObservableProperty]
    private int _lifeTotal = 20;

    [ObservableProperty]
    private int _whiteMana = 0;

    [ObservableProperty]
    private int _blueMana = 0;

    [ObservableProperty]
    private int _blackMana = 0;

    [ObservableProperty]
    private int _redMana = 0;

    [ObservableProperty]
    private int _greenMana = 0;

    [ObservableProperty]
    private int _colorlessMana = 0;

    [ObservableProperty]
    private int _stormCount = 0;

    [ObservableProperty]
    private int _energyCount = 0;

    [ObservableProperty]
    private int _poisonCount = 0;

    [ObservableProperty]
    private int _commanderTax = 0;

    [ObservableProperty]
    private int _commanderDamage = 0;

    /// <summary>
    /// Whether this game was started with a Commander deck
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCommandZone))]
    private bool _isCommanderGame;

    // Phase tracking
    [ObservableProperty]
    private GamePhase _currentPhase = GamePhase.Untap;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseDisplayName))]
    private int _currentPhaseIndex = 0;

    public bool HasCommandZone => IsCommanderGame;

    public bool HasSideboard => Sideboard.Count > 0;

    public string PhaseDisplayName => CurrentPhase switch
    {
        GamePhase.Untap => "Untap",
        GamePhase.Upkeep => "Upkeep",
        GamePhase.Draw => "Draw",
        GamePhase.MainPhase1 => "Main",
        GamePhase.BeginCombat => "Begin Combat",
        GamePhase.DeclareAttackers => "Attack",
        GamePhase.DeclareBlockers => "Block",
        GamePhase.CombatDamage => "Damage",
        GamePhase.EndCombat => "End Combat",
        GamePhase.MainPhase2 => "Main",
        GamePhase.End => "End",
        GamePhase.Cleanup => "Pass",
        _ => CurrentPhase.ToString()
    };

    // Selected card for details panel
    [ObservableProperty]
    private PlaytestCardViewModel? _selectedCard;

    [ObservableProperty]
    private int _mulliganCount = 0;

    // Per-zone card scaling (1.0 = 100x140 base size)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardWidth), nameof(CardHeight), nameof(CardFontSizeName), nameof(CardFontSizeText), nameof(CardFontSizePT), nameof(CardFontSizeMana), nameof(DetailsImageMaxHeight), nameof(StackCardWidth), nameof(StackCardHeight))]
    private double _battlefieldCardScale = 1.25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LandsCardWidth), nameof(LandsCardHeight), nameof(LandsFontSizeName), nameof(LandsFontSizeText), nameof(LandsFontSizePT))]
    private double _landsCardScale = 1.25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HandCardWidth), nameof(HandCardHeight), nameof(HandFontSizeName), nameof(HandFontSizeMana), nameof(HandFontSizePT))]
    private double _handCardScale = 1.25;

    // Battlefield computed dimensions (base: 100x140)
    public double CardWidth => 100 * BattlefieldCardScale;
    public double CardHeight => 140 * BattlefieldCardScale;
    public double CardFontSizeName => 8 * BattlefieldCardScale;
    public double CardFontSizeText => 7 * BattlefieldCardScale;
    public double CardFontSizePT => 8 * BattlefieldCardScale;
    public double CardFontSizeMana => 8 * BattlefieldCardScale;
    public double DetailsImageMaxHeight => 250 * BattlefieldCardScale;
    public double StackCardWidth => 40 * BattlefieldCardScale;
    public double StackCardHeight => 56 * BattlefieldCardScale;

    // Lands zone computed dimensions
    public double LandsCardWidth => 100 * LandsCardScale;
    public double LandsCardHeight => 140 * LandsCardScale;
    public double LandsFontSizeName => 8 * LandsCardScale;
    public double LandsFontSizeText => 7 * LandsCardScale;
    public double LandsFontSizePT => 8 * LandsCardScale;

    // Hand zone computed dimensions
    public double HandCardWidth => 100 * HandCardScale;
    public double HandCardHeight => 140 * HandCardScale;
    public double HandFontSizeName => 8 * HandCardScale;
    public double HandFontSizeMana => 8 * HandCardScale;
    public double HandFontSizePT => 8 * HandCardScale;

    // Game action log
    public ObservableCollection<GameLogEntry> GameLog { get; } = new();

    // Library card count
    public int LibraryCount => Library.Count;

    public PlaytestCardViewModel? TopGraveyardCard => Graveyard.FirstOrDefault();

    public PlaytestCardViewModel? TopExileCard => Exile.FirstOrDefault();

    // Counter increment/decrement commands
    [RelayCommand]
    private void IncrementLife()
    {
        LifeTotal++;
        AddLogEntry($"Player's life is now {LifeTotal} (+1)");
    }

    [RelayCommand]
    private void DecrementLife()
    {
        LifeTotal--;
        AddLogEntry($"Player's life is now {LifeTotal} (-1)");
    }

    [RelayCommand]
    private void IncrementWhiteMana() => WhiteMana++;

    [RelayCommand]
    private void DecrementWhiteMana() => WhiteMana = Math.Max(0, WhiteMana - 1);

    [RelayCommand]
    private void IncrementBlueMana() => BlueMana++;

    [RelayCommand]
    private void DecrementBlueMana() => BlueMana = Math.Max(0, BlueMana - 1);

    [RelayCommand]
    private void IncrementBlackMana() => BlackMana++;

    [RelayCommand]
    private void DecrementBlackMana() => BlackMana = Math.Max(0, BlackMana - 1);

    [RelayCommand]
    private void IncrementRedMana() => RedMana++;

    [RelayCommand]
    private void DecrementRedMana() => RedMana = Math.Max(0, RedMana - 1);

    [RelayCommand]
    private void IncrementGreenMana() => GreenMana++;

    [RelayCommand]
    private void DecrementGreenMana() => GreenMana = Math.Max(0, GreenMana - 1);

    [RelayCommand]
    private void IncrementColorlessMana() => ColorlessMana++;

    [RelayCommand]
    private void DecrementColorlessMana() => ColorlessMana = Math.Max(0, ColorlessMana - 1);

    [RelayCommand]
    private void IncrementStorm()
    {
        StormCount++;
        AddLogEntry($"Player's Storm is now {StormCount} (+1)");
    }

    [RelayCommand]
    private void DecrementStorm()
    {
        var prev = StormCount;
        StormCount = Math.Max(0, StormCount - 1);
        if (StormCount != prev)
            AddLogEntry($"Player's Storm is now {StormCount} (-1)");
    }

    [RelayCommand]
    private void IncrementEnergy()
    {
        EnergyCount++;
        AddLogEntry($"Player's Energy is now {EnergyCount} (+1)");
    }

    [RelayCommand]
    private void DecrementEnergy()
    {
        var prev = EnergyCount;
        EnergyCount = Math.Max(0, EnergyCount - 1);
        if (EnergyCount != prev)
            AddLogEntry($"Player's Energy is now {EnergyCount} (-1)");
    }

    [RelayCommand]
    private void IncrementPoison()
    {
        PoisonCount++;
        AddLogEntry($"Player's Poison is now {PoisonCount} (+1)");
    }

    [RelayCommand]
    private void DecrementPoison()
    {
        var prev = PoisonCount;
        PoisonCount = Math.Max(0, PoisonCount - 1);
        if (PoisonCount != prev)
            AddLogEntry($"Player's Poison is now {PoisonCount} (-1)");
    }

    [RelayCommand]
    private void IncrementCommanderTax()
    {
        CommanderTax++;
        AddLogEntry($"Player's Commander Tax is now {CommanderTax} (+1)");
    }

    [RelayCommand]
    private void DecrementCommanderTax()
    {
        var prev = CommanderTax;
        CommanderTax = Math.Max(0, CommanderTax - 1);
        if (CommanderTax != prev)
            AddLogEntry($"Player's Commander Tax is now {CommanderTax} (-1)");
    }

    [RelayCommand]
    private void IncrementCommanderDamage()
    {
        CommanderDamage++;
        AddLogEntry($"Player's Commander Damage is now {CommanderDamage} (+1)");
    }

    [RelayCommand]
    private void DecrementCommanderDamage()
    {
        var prev = CommanderDamage;
        CommanderDamage = Math.Max(0, CommanderDamage - 1);
        if (CommanderDamage != prev)
            AddLogEntry($"Player's Commander Damage is now {CommanderDamage} (-1)");
    }

    /// <summary>
    /// Initialize the game with a deck
    /// </summary>
    public async Task InitializeWithDeck(DeckModel deck, ICollectionTrackingService service)
    {
        ResetGame();
        MulliganCount = 0;
        IsCommanderGame = deck.IsCommander;

        // Load all cards from main deck
        var allCards = new List<PlaytestCard>();

        foreach (var deckCard in deck.MainDeck)
        {
            var card = new PlaytestCard
            {
                CardName = deckCard.CardName,
                ScryfallId = deckCard.ScryfallId,
                ScryfallIdBack = deckCard.IsDoubleFaced ? deckCard.ScryfallId : null,
                ManaCost = deckCard.CastingCost,
                CardType = deckCard.CardType,
                Power = deckCard.Power,
                Toughness = deckCard.Toughness,
                OracleText = deckCard.OracleText,
                IsLand = deckCard.IsLand,
                IsDoubleFaced = deckCard.IsDoubleFaced,
                Zone = GameZone.Library,
                IsTapped = false,
                IsFrontFace = true
            };

            allCards.Add(card);
        }

        // Shuffle and add to library
        ShuffleCards(allCards);

        foreach (var card in allCards)
        {
            var vm = _cardVmFactory();
            vm.InitializeFrom(card);
            Library.Add(vm);
        }

        // If this is a commander deck and has a commander, place it in the command zone
        if (deck.IsCommander && deck.Commander != null)
        {
            var cmdCard = new PlaytestCard
            {
                CardName = deck.Commander.CardName,
                ScryfallId = deck.Commander.ScryfallId,
                ScryfallIdBack = deck.Commander.IsDoubleFaced ? deck.Commander.ScryfallId : null,
                ManaCost = deck.Commander.CastingCost,
                CardType = deck.Commander.CardType,
                Power = deck.Commander.Power,
                Toughness = deck.Commander.Toughness,
                OracleText = deck.Commander.OracleText,
                IsLand = deck.Commander.IsLand,
                IsDoubleFaced = deck.Commander.IsDoubleFaced,
                Zone = GameZone.CommandZone,
                IsTapped = false,
                IsFrontFace = true,
                IsCommanderCard = true
            };
            var cmdVm = _cardVmFactory();
            cmdVm.InitializeFrom(cmdCard);
            CommandZone.Add(cmdVm);
        }

        // Load sideboard cards
        _originalSideboardCards.Clear();
        Sideboard.Clear();
        foreach (var deckCard in deck.Sideboard)
        {
            var sbCard = new PlaytestCard
            {
                CardName = deckCard.CardName,
                ScryfallId = deckCard.ScryfallId,
                ScryfallIdBack = deckCard.IsDoubleFaced ? deckCard.ScryfallId : null,
                ManaCost = deckCard.CastingCost,
                CardType = deckCard.CardType,
                Power = deckCard.Power,
                Toughness = deckCard.Toughness,
                OracleText = deckCard.OracleText,
                IsLand = deckCard.IsLand,
                IsDoubleFaced = deckCard.IsDoubleFaced,
                Zone = GameZone.Sideboard,
                IsTapped = false,
                IsFrontFace = true
            };
            _originalSideboardCards.Add(sbCard);
            var sbVm = _cardVmFactory();
            sbVm.InitializeFrom(sbCard);
            Sideboard.Add(sbVm);
        }

        OnPropertyChanged(nameof(LibraryCount));
        OnPropertyChanged(nameof(HasSideboard));
    }

    /// <summary>
    /// Reset the game to initial state
    /// </summary>
    [RelayCommand]
    private void ResetGame()
    {
        // Collect all cards from all zones
        var allCards = new List<PlaytestCardViewModel>();
        allCards.AddRange(Library);
        allCards.AddRange(Hand);
        allCards.AddRange(Graveyard);
        allCards.AddRange(Exile);
        allCards.AddRange(Stack);
        allCards.AddRange(BattlefieldLands);
        allCards.AddRange(BattlefieldNonlands);
        allCards.AddRange(CommandZone);
        allCards.AddRange(Sideboard);

        // Clear all zones
        Library.Clear();
        Hand.Clear();
        Graveyard.Clear();
        Exile.Clear();
        Stack.Clear();
        BattlefieldLands.Clear();
        BattlefieldNonlands.Clear();
        CommandZone.Clear();
        Sideboard.Clear();
        OnPropertyChanged(nameof(HasSideboard));
        SelectedBattlefieldCards.Clear();

        // Sort cards: commander cards go to Command Zone, sideboard cards go back to sideboard, others go to library (tokens are discarded)
        foreach (var card in allCards)
        {
            if (card.IsToken)
            {
                continue;
            }

            card.IsTapped = false;
            card.IsFrontFace = true;
            card.IsSelected = false;
            card.LoadCardImages();

            if (card.IsCommanderCard)
            {
                card.Zone = GameZone.CommandZone;
                CommandZone.Add(card);
            }
            else if (card.Zone == GameZone.Sideboard)
            {
                card.Zone = GameZone.Sideboard;
                Sideboard.Add(card);
            }
            else
            {
                card.Zone = GameZone.Library;
                Library.Add(card);
            }
        }

        // Re-add any sideboard cards that were moved to other zones (restore from original)
        // Use count-based tracking to handle multiple copies of the same card correctly
        var presentSideboardCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in Sideboard)
        {
            presentSideboardCounts.TryGetValue(c.CardName, out var cnt);
            presentSideboardCounts[c.CardName] = cnt + 1;
        }

        var originalCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var orig in _originalSideboardCards)
        {
            originalCounts.TryGetValue(orig.CardName, out var cnt);
            originalCounts[orig.CardName] = cnt + 1;
        }

        foreach (var orig in _originalSideboardCards)
        {
            presentSideboardCounts.TryGetValue(orig.CardName, out var present);
            originalCounts.TryGetValue(orig.CardName, out var needed);
            if (present < needed)
            {
                // Prefer to reuse an existing card VM (likely currently in Library) rather than creating a duplicate.
                var existingVm = Library.FirstOrDefault(
                    c => string.Equals(c.CardName, orig.CardName, StringComparison.OrdinalIgnoreCase));

                if (existingVm != null)
                {
                    Library.Remove(existingVm);
                    existingVm.Zone = GameZone.Sideboard;
                    Sideboard.Add(existingVm);
                    OnPropertyChanged(nameof(LibraryCount));
                }
                else
                {
                    var sbVm = _cardVmFactory();
                    sbVm.InitializeFrom(orig);
                    Sideboard.Add(sbVm);
                }

                presentSideboardCounts[orig.CardName] = present + 1;
            }
        }

        // Shuffle library
        ShuffleLibrary();

        // Reset counters
        LifeTotal = 20;
        WhiteMana = 0;
        BlueMana = 0;
        BlackMana = 0;
        RedMana = 0;
        GreenMana = 0;
        ColorlessMana = 0;
        StormCount = 0;
        EnergyCount = 0;
        PoisonCount = 0;
        CommanderTax = 0;
        CommanderDamage = 0;

        // Reset phase
        CurrentPhase = GamePhase.Untap;
        CurrentPhaseIndex = 0;

        MulliganCount = 0;

        SelectedCard = null;
        OnPropertyChanged(nameof(LibraryCount));
        OnPropertyChanged(nameof(HasSideboard));

        // Clear the game log on reset
        GameLog.Clear();
    }

    /// <summary>
    /// Shuffle the library
    /// </summary>
    [RelayCommand]
    private void ShuffleLibrary()
    {
        var cards = Library.ToList();
        ShuffleCards(cards);
        Library.Clear();
        foreach (var card in cards)
        {
            Library.Add(card);
        }

        AddLogEntry("Player shuffles library");
    }

    /// <summary>
    /// Draw a single card from library to hand. Does not emit a log entry — caller is responsible for logging.
    /// </summary>
    private void DrawCardInternal()
    {
        if (Library.Count == 0) return;

        var card = Library[0];
        Library.RemoveAt(0);
        card.Zone = GameZone.Hand;
        Hand.Add(card);
        OnPropertyChanged(nameof(LibraryCount));
    }

    /// <summary>
    /// Draw a card command — draws one card from library to hand
    /// </summary>
    [RelayCommand]
    private void DrawCard()
    {
        if (Library.Count == 0) return;

        DrawCardInternal();
        AddLogEntry("Player draws 1 card");
    }

    /// <summary>
    /// Draw X cards from library
    /// </summary>
    public void DrawCards(int count)
    {
        int drawn = 0;
        for (int i = 0; i < count && Library.Count > 0; i++)
        {
            DrawCardInternal();
            drawn++;
        }

        if (drawn > 0)
            AddLogEntry($"Player draws {drawn} card{(drawn != 1 ? "s" : "")}");
    }

    /// <summary>
    /// Mulligan: return hand to library, shuffle, then draw 7 cards
    /// </summary>
    [RelayCommand]
    private void Mulligan()
    {
        if (Hand.Count > 0)
        {
            var cards = Hand.ToList();
            Hand.Clear();
            foreach (var card in cards)
            {
                card.Zone = GameZone.Library;
                Library.Add(card);
            }
            OnPropertyChanged(nameof(LibraryCount));
        }

        ShuffleLibrary();
        DrawCards(7);

        var pendingCount = MulliganCount + 1;
        if (pendingCount > 0)
        {
            var bottomCount = Math.Min(pendingCount, Hand.Count);
            if (bottomCount == 0)
            {
                MulliganCount = pendingCount;
                AddLogEntry($"Player mulligans to {Hand.Count} cards");
                return;
            }

            var dialog = new DialogViewModel(_messenger).WithContent(
                "Mulligan - Put Cards on Bottom",
                new MulliganSelectionViewModel().Configure(
                    Hand,
                    bottomCount,
                    selected =>
                    {
                        MulliganCount = pendingCount;
                        foreach (var card in selected)
                        {
                            Hand.Remove(card);
                            card.Zone = GameZone.Library;
                            Library.Add(card);
                        }

                        OnPropertyChanged(nameof(LibraryCount));
                        AddLogEntry($"Player mulligans to {Hand.Count} cards");
                    }));

            dialog.CanClose = false;

            _messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 520,
                ViewModel = dialog,
            });
        }
    }

    /// <summary>
    /// Mill X cards from library to graveyard
    /// </summary>
    public void MillCards(int count)
    {
        int milled = 0;
        for (int i = 0; i < count && Library.Count > 0; i++)
        {
            var card = Library[0];
            Library.RemoveAt(0);
            card.Zone = GameZone.Graveyard;
            Graveyard.Insert(0, card);
            milled++;
        }
        OnPropertyChanged(nameof(LibraryCount));

        if (milled > 0)
            AddLogEntry($"Player mills {milled} card{(milled != 1 ? "s" : "")}");
    }

    public void OpenZoneContentsDialog(GameZone sourceZone)
    {
        var sourceCards = sourceZone switch
        {
            GameZone.Library => Library,
            GameZone.Graveyard => Graveyard,
            GameZone.Exile => Exile,
            _ => null
        };

        if (sourceCards is null)
        {
            return;
        }

        AddLogEntry($"Player is viewing {GetZoneDisplayName(sourceZone)}");

        var viewModel = new ZoneContentsViewModel(_messenger).Configure(
            sourceZone,
            sourceCards,
            (card, targetZone) => MoveCard(card, targetZone),
            sourceZone == GameZone.Library ? ShuffleLibrary : null);

        var dialog = new DialogViewModel(_messenger).WithContent(
            $"{sourceZone} - View Contents",
            viewModel);

        dialog.CanClose = true;

        _messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 620,
            ViewModel = dialog,
        });
    }

    /// <summary>
    /// Opens a dialog showing the sideboard. Cards can be moved from sideboard to hand.
    /// </summary>
    public void OpenSideboardDialog()
    {
        if (Sideboard.Count == 0)
            return;

        AddLogEntry($"Player is viewing {GetZoneDisplayName(GameZone.Sideboard)}");

        var viewModel = new ZoneContentsViewModel(_messenger).Configure(
            GameZone.Sideboard,
            Sideboard,
            (card, targetZone) => MoveCard(card, targetZone),
            null);

        var dialog = new DialogViewModel(_messenger).WithContent(
            "Sideboard",
            viewModel);

        dialog.CanClose = true;

        _messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 620,
            ViewModel = dialog,
        });
    }

    /// <summary>
    /// Opens a dialog showing the command zone. The commander can be moved to the Stack, Hand, Battlefield, Graveyard, or Library.
    /// </summary>
    public void OpenCommandZoneDialog()
    {
        if (CommandZone.Count == 0)
            return;

        AddLogEntry($"Player is viewing {GetZoneDisplayName(GameZone.CommandZone)}");

        var viewModel = new CommandZoneViewModel(_messenger).Configure(
            CommandZone,
            (card, targetZone) => MoveCard(card, targetZone),
            moveToTopOfLibrary: card =>
            {
                RemoveFromZone(card, GameZone.CommandZone);
                card.Zone = GameZone.Library;
                Library.Insert(0, card);
                OnPropertyChanged(nameof(LibraryCount));
            },
            moveToBottomOfLibrary: card =>
            {
                RemoveFromZone(card, GameZone.CommandZone);
                card.Zone = GameZone.Library;
                Library.Add(card);
                OnPropertyChanged(nameof(LibraryCount));
            },
            moveToLibraryAndShuffle: card =>
            {
                RemoveFromZone(card, GameZone.CommandZone);
                card.Zone = GameZone.Library;
                Library.Add(card);
                OnPropertyChanged(nameof(LibraryCount));
                ShuffleLibrary();
            });

        var dialog = new DialogViewModel(_messenger).WithContent(
            "Command Zone",
            viewModel);

        dialog.CanClose = true;

        _messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 520,
            ViewModel = dialog,
        });
    }

    public void OpenCreateTokenDialog()
    {
        var viewModel = new CreateTokenViewModel(_messenger).Configure(CreateTokenOnBattlefield);

        var dialog = new DialogViewModel(_messenger).WithContent(
            "Create Token",
            viewModel);

        dialog.CanClose = true;

        _messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 520,
            ViewModel = dialog,
        });
    }

    public void OpenAddCounterDialog(PlaytestCardViewModel card)
    {
        var viewModel = new AddCounterViewModel(_messenger).Configure((name, color, qty) =>
        {
            var counter = card.Counters.FirstOrDefault(c =>
                string.Equals(c.CounterName, name, StringComparison.OrdinalIgnoreCase));

            if (counter is not null)
            {
                counter.Quantity += qty;
            }
            else
            {
                card.Counters.Add(new CardCounterViewModel
                {
                    CounterName = name,
                    CounterColor = color,
                    Quantity = qty,
                });
            }

            var plural = qty != 1 ? "s" : "";
            AddLogEntry($"Player puts {qty} {name} counter{plural} on {card.CardName}");
        });

        var dialog = new DialogViewModel(_messenger).WithContent("Add Counter", viewModel);
        dialog.CanClose = true;

        _messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 480,
            ViewModel = dialog,
        });
    }

    public void OpenViewTopXDialog(int topX)
    {
        if (Library.Count == 0)
            return;

        var topCards = Library.Take(topX).ToList();
        AddLogEntry($"Player views top {topCards.Count} card{(topCards.Count != 1 ? "s" : "")} of [{GetZoneDisplayName(GameZone.Library)}]");

        var viewModel = new ViewTopXViewModel(_messenger).Configure(
            topCards,
            () => ShuffleLibrary(),
            cards => ReorderTopCards(cards),
            card => MoveCardsToHand([card]),
            card => MoveCardsToGraveyard([card], CardMoveOrder.AsSelected),
            card => MoveCardsToExile([card]),
            card => MoveCardsToBottomOfLibrary([card], CardMoveOrder.AsSelected));

        var dialog = new DialogViewModel(_messenger).WithContent(
            $"View Top {topX} Cards",
            viewModel);

        dialog.CanClose = true;

        _messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 680,
            ViewModel = dialog,
        });
    }

    private void MoveCardsToHand(IEnumerable<PlaytestCardViewModel> cards)
    {
        foreach (var card in cards.ToList())
        {
            Library.Remove(card);
            card.Zone = GameZone.Hand;
            Hand.Add(card);
        }
        OnPropertyChanged(nameof(LibraryCount));
    }

    private void MoveCardsToGraveyard(IEnumerable<PlaytestCardViewModel> cards, CardMoveOrder order)
    {
        var list = cards.ToList();
        if (order == CardMoveOrder.Random)
            ShuffleCards(list);

        for (var index = list.Count - 1; index >= 0; index--)
        {
            var card = list[index];
            Library.Remove(card);
            card.Zone = GameZone.Graveyard;
            Graveyard.Insert(0, card);
        }
        OnPropertyChanged(nameof(LibraryCount));
    }

    private void MoveCardsToExile(IEnumerable<PlaytestCardViewModel> cards)
    {
        foreach (var card in cards.ToList())
        {
            Library.Remove(card);
            card.Zone = GameZone.Exile;
            Exile.Insert(0, card);
        }
        OnPropertyChanged(nameof(LibraryCount));
    }

    private void MoveCardsToBottomOfLibrary(IEnumerable<PlaytestCardViewModel> cards, CardMoveOrder order)
    {
        var list = cards.ToList();
        if (order == CardMoveOrder.Random)
            ShuffleCards(list);

        foreach (var card in list)
        {
            Library.Remove(card);
            card.Zone = GameZone.Library;
            Library.Add(card);
        }
        // count notification not needed since cards stay in library
        OnPropertyChanged(nameof(LibraryCount));
    }

    private void MoveCardsToTopOfLibrary(IEnumerable<PlaytestCardViewModel> cards, CardMoveOrder order)
    {
        var list = cards.ToList();
        if (order == CardMoveOrder.Random)
            ShuffleCards(list);

        // Insert in reverse so that the first selected card ends up on top
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var card = list[i];
            Library.Remove(card);
            card.Zone = GameZone.Library;
            Library.Insert(0, card);
        }
        OnPropertyChanged(nameof(LibraryCount));
    }

    private void ReorderTopCards(IReadOnlyList<PlaytestCardViewModel> cards)
    {
        if (cards.Count <= 1)
        {
            return;
        }

        foreach (var card in cards)
        {
            Library.Remove(card);
        }

        for (var index = cards.Count - 1; index >= 0; index--)
        {
            var card = cards[index];
            card.Zone = GameZone.Library;
            Library.Insert(0, card);
        }

        OnPropertyChanged(nameof(LibraryCount));
    }

    /// <summary>
    /// Exile X cards from the top of the library
    /// </summary>
    public void ExileTopCards(int count)
    {
        for (int i = 0; i < count && Library.Count > 0; i++)
        {
            var card = Library[0];
            Library.RemoveAt(0);
            card.Zone = GameZone.Exile;
            Exile.Insert(0, card);
            LogZoneMove(card.CardName, GameZone.Library, GameZone.Exile);
        }
        OnPropertyChanged(nameof(LibraryCount));
    }

    /// <summary>
    /// Advance to the next phase
    /// </summary>
    [RelayCommand]
    private void AdvancePhase()
    {
        // Special actions for certain phases
        if (CurrentPhase == GamePhase.Untap)
        {
            UntapAll();
        }
        else if (CurrentPhase == GamePhase.Cleanup)
        {
            // Pass - return to Untap
            CurrentPhase = GamePhase.Untap;
            CurrentPhaseIndex = 0;
            AddLogEntry($"Player advances to phase ({PhaseDisplayName})");
            return;
        }

        // Move to next phase
        CurrentPhaseIndex++;
        if (CurrentPhaseIndex > 11)
        {
            CurrentPhaseIndex = 0;
        }
        CurrentPhase = (GamePhase)CurrentPhaseIndex;
        AddLogEntry($"Player advances to phase ({PhaseDisplayName})");
    }

    /// <summary>
    /// End turn - reset to Untap phase
    /// </summary>
    [RelayCommand]
    private void EndTurn()
    {
        CurrentPhase = GamePhase.Untap;
        CurrentPhaseIndex = 0;
    }

    /// <summary>
    /// Untap all cards on the battlefield
    /// </summary>
    private void UntapAll()
    {
        foreach (var card in BattlefieldLands.Concat(BattlefieldNonlands))
        {
            if (card.IsTapped)
            {
                card.IsTapped = false;
            }
        }
    }

    /// <summary>
    /// Move a card between zones
    /// </summary>
    public void MoveCard(PlaytestCardViewModel card, GameZone targetZone)
    {
        var sourceZone = card.Zone;

        // If a selected card is leaving the battlefield, remove it from the selection immediately.
        if (IsBattlefieldZone(sourceZone) && card.IsSelected)
        {
            card.IsSelected = false;
            SelectedBattlefieldCards.Remove(card);
        }

        // Remove from current zone
        RemoveFromZone(card, sourceZone);

        if (ShouldTokenCeaseToExist(card, sourceZone, targetZone))
        {
            if (ReferenceEquals(SelectedCard, card))
            {
                SelectedCard = null;
            }

            return;
        }

        // Add to target zone
        card.Zone = targetZone;
        AddToZone(card, targetZone);

        LogZoneMove(card.CardName, sourceZone, targetZone);
    }

    private static bool ShouldTokenCeaseToExist(PlaytestCardViewModel card, GameZone sourceZone, GameZone targetZone)
    {
        return card.IsToken &&
               IsBattlefieldZone(sourceZone) &&
               !IsBattlefieldZone(targetZone);
    }

    private static bool IsBattlefieldZone(GameZone zone)
    {
        return zone == GameZone.Battlefield || zone == GameZone.BattlefieldLands;
    }

    /// <summary>
    /// Toggle selection of a battlefield card. If ctrl is not held, clears previous selection first.
    /// </summary>
    public void ToggleBattlefieldCardSelection(PlaytestCardViewModel card, bool addToSelection)
    {
        if (!addToSelection)
        {
            // Clear existing selection, but keep the clicked card selected.
            foreach (var existing in SelectedBattlefieldCards.ToList())
            {
                if (!ReferenceEquals(existing, card))
                {
                    existing.IsSelected = false;
                }
            }
            SelectedBattlefieldCards.Clear();

            card.IsSelected = true;
            if (!SelectedBattlefieldCards.Contains(card))
            {
                SelectedBattlefieldCards.Add(card);
            }

            return;
        }

        card.IsSelected = !card.IsSelected;
        if (card.IsSelected)
        {
            if (!SelectedBattlefieldCards.Contains(card))
                SelectedBattlefieldCards.Add(card);
        }
        else
        {
            SelectedBattlefieldCards.Remove(card);
        }
    }

    /// <summary>
    /// Remove any cards from SelectedBattlefieldCards that are no longer on the battlefield,
    /// keeping IsSelected in sync with the selection collection.
    /// </summary>
    private void PruneSelectedBattlefieldCards()
    {
        foreach (var card in SelectedBattlefieldCards.ToList())
        {
            if (!IsBattlefieldZone(card.Zone))
            {
                card.IsSelected = false;
                SelectedBattlefieldCards.Remove(card);
            }
        }
    }

    /// <summary>
    /// Tap all currently selected battlefield cards
    /// </summary>
    public void TapSelectedCards()
    {
        PruneSelectedBattlefieldCards();
        foreach (var card in SelectedBattlefieldCards.ToList())
        {
            card.IsTapped = true;
            AddLogEntry($"Player taps {card.CardName}");
        }
    }

    /// <summary>
    /// Untap all currently selected battlefield cards
    /// </summary>
    public void UntapSelectedCards()
    {
        PruneSelectedBattlefieldCards();
        foreach (var card in SelectedBattlefieldCards.ToList())
        {
            card.IsTapped = false;
            AddLogEntry($"Player untaps {card.CardName}");
        }
    }

    /// <summary>
    /// Move all selected battlefield cards to the specified zone
    /// </summary>
    public void MoveSelectedBattlefieldCardsTo(GameZone targetZone, CardMoveOrder order = CardMoveOrder.AsSelected)
    {
        PruneSelectedBattlefieldCards();
        var toMove = SelectedBattlefieldCards.ToList();
        if (order == CardMoveOrder.Random)
        {
            ShuffleCards(toMove);
        }

        if (TargetZoneInsertsAtTop(targetZone))
        {
            toMove.Reverse();
        }

        foreach (var card in toMove)
        {
            card.IsSelected = false;
            MoveCard(card, targetZone);
        }
        SelectedBattlefieldCards.Clear();
    }

    private static bool TargetZoneInsertsAtTop(GameZone targetZone)
    {
        return targetZone == GameZone.Graveyard ||
               targetZone == GameZone.Exile ||
               targetZone == GameZone.Stack;
    }

    /// <summary>
    /// Adjust an existing counter on a card by delta (+1 or -1)
    /// </summary>
    public void AdjustCounter(PlaytestCardViewModel card, string counterName, int delta)
    {
        var counter = card.Counters.FirstOrDefault(c =>
            string.Equals(c.CounterName, counterName, StringComparison.OrdinalIgnoreCase));

        if (counter is null)
            return;

        counter.Quantity += delta;

        var abs = Math.Abs(delta);
        var plural = abs != 1 ? "s" : "";
        if (delta > 0)
            AddLogEntry($"Player puts {abs} {counterName} counter{plural} on {card.CardName}");
        else if (delta < 0)
            AddLogEntry($"Player removes {abs} {counterName} counter{plural} from {card.CardName}");

        if (counter.Quantity <= 0)
        {
            card.Counters.Remove(counter);
        }
    }

    private void CreateTokenOnBattlefield(string name, string? oracleText, string power, string toughness)
    {
        var tokenModel = new PlaytestCard
        {
            CardName = name,
            OracleText = oracleText,
            CardType = "Token",
            Power = power,
            Toughness = toughness,
            IsLand = false,
            IsDoubleFaced = false,
            IsToken = true,
            IsTapped = false,
            IsFrontFace = true,
            Zone = GameZone.Battlefield,
        };

        var token = _cardVmFactory();
        token.InitializeFrom(tokenModel);
        BattlefieldNonlands.Add(token);
        SelectedCard = token;

        AddLogEntry($"Player creates a token ({name})");
    }

    /// <summary>
    /// Send a card from hand to the bottom of the library
    /// </summary>
    public void SendToBottomOfLibrary(PlaytestCardViewModel card)
    {
        if (card.Zone != GameZone.Hand) return;

        Hand.Remove(card);
        card.Zone = GameZone.Library;
        Library.Add(card);
        OnPropertyChanged(nameof(LibraryCount));
        LogZoneMove(card.CardName, GameZone.Hand, GameZone.Library);
    }

    /// <summary>
    /// Send a card from hand to the top of the library
    /// </summary>
    public void SendToTopOfLibrary(PlaytestCardViewModel card)
    {
        if (card.Zone != GameZone.Hand) return;

        Hand.Remove(card);
        card.Zone = GameZone.Library;
        Library.Insert(0, card);
        OnPropertyChanged(nameof(LibraryCount));
        LogZoneMove(card.CardName, GameZone.Hand, GameZone.Library);
    }

    /// <summary>
    /// Discard a card from hand to the top of the graveyard
    /// </summary>
    public void DiscardFromHand(PlaytestCardViewModel card)
    {
        if (card.Zone != GameZone.Hand) return;

        Hand.Remove(card);
        card.Zone = GameZone.Graveyard;
        Graveyard.Insert(0, card);
        LogZoneMove(card.CardName, GameZone.Hand, GameZone.Graveyard);
    }

    /// <summary>
    /// Exile a card from hand
    /// </summary>
    public void ExileFromHand(PlaytestCardViewModel card)
    {
        if (card.Zone != GameZone.Hand) return;

        Hand.Remove(card);
        card.Zone = GameZone.Exile;
        Exile.Insert(0, card);
        LogZoneMove(card.CardName, GameZone.Hand, GameZone.Exile);
    }

    /// <summary>
    /// Play a card from hand
    /// </summary>
    public void PlayCardFromHand(PlaytestCardViewModel card)
    {
        if (card.Zone != GameZone.Hand) return;

        Hand.Remove(card);

        var isLandType = card.IsLand ||
                         (!string.IsNullOrWhiteSpace(card.CardType) &&
                          card.CardType.Contains("Land", StringComparison.OrdinalIgnoreCase));

        if (isLandType)
        {
            // Lands go directly to battlefield
            card.Zone = GameZone.BattlefieldLands;
            BattlefieldLands.Add(card);
            AddLogEntry($"Player puts {card.CardName} onto the battlefield");
        }
        else
        {
            // Spells go to stack
            card.Zone = GameZone.Stack;
            Stack.Insert(0, card);
            AddLogEntry($"Player puts {card.CardName} onto the stack");
        }
    }

    /// <summary>
    /// Resolve the top card of the stack
    /// </summary>
    public void ResolveStack()
    {
        if (Stack.Count == 0) return;

        var card = Stack[0];
        Stack.RemoveAt(0);
        card.Zone = GameZone.Battlefield;
        BattlefieldNonlands.Add(card);
        AddLogEntry($"Player resolves {card.CardName}");
    }

    /// <summary>
    /// Counter the top spell on the stack (send to graveyard)
    /// </summary>
    public void CounterStack()
    {
        if (Stack.Count == 0) return;

        var card = Stack[0];
        Stack.RemoveAt(0);
        card.Zone = GameZone.Graveyard;
        Graveyard.Insert(0, card);
        LogZoneMove(card.CardName, GameZone.Stack, GameZone.Graveyard);
    }

    /// <summary>
    /// Return the top card of the stack to hand
    /// </summary>
    public void ReturnStackToHand()
    {
        if (Stack.Count == 0) return;

        var card = Stack[0];
        Stack.RemoveAt(0);
        card.Zone = GameZone.Hand;
        Hand.Add(card);
        LogZoneMove(card.CardName, GameZone.Stack, GameZone.Hand);
    }

    /// <summary>
    /// Exile the top card of the stack
    /// </summary>
    public void ExileFromStack()
    {
        if (Stack.Count == 0) return;

        var card = Stack[0];
        Stack.RemoveAt(0);
        card.Zone = GameZone.Exile;
        Exile.Insert(0, card);
        LogZoneMove(card.CardName, GameZone.Stack, GameZone.Exile);
    }

    private void RemoveFromZone(PlaytestCardViewModel card, GameZone zone)
    {
        switch (zone)
        {
            case GameZone.Library:
                Library.Remove(card);
                OnPropertyChanged(nameof(LibraryCount));
                break;
            case GameZone.Hand:
                Hand.Remove(card);
                break;
            case GameZone.Graveyard:
                Graveyard.Remove(card);
                break;
            case GameZone.Exile:
                Exile.Remove(card);
                break;
            case GameZone.Stack:
                Stack.Remove(card);
                break;
            case GameZone.BattlefieldLands:
                BattlefieldLands.Remove(card);
                break;
            case GameZone.Battlefield:
                BattlefieldNonlands.Remove(card);
                break;
            case GameZone.CommandZone:
                CommandZone.Remove(card);
                break;
            case GameZone.Sideboard:
                Sideboard.Remove(card);
                OnPropertyChanged(nameof(HasSideboard));
                break;
        }
    }

    private void AddToZone(PlaytestCardViewModel card, GameZone zone)
    {
        switch (zone)
        {
            case GameZone.Library:
                Library.Add(card);
                OnPropertyChanged(nameof(LibraryCount));
                break;
            case GameZone.Hand:
                Hand.Add(card);
                break;
            case GameZone.Graveyard:
                Graveyard.Insert(0, card);
                break;
            case GameZone.Exile:
                Exile.Insert(0, card);
                break;
            case GameZone.Stack:
                Stack.Insert(0, card);
                break;
            case GameZone.BattlefieldLands:
                BattlefieldLands.Add(card);
                break;
            case GameZone.Battlefield:
                BattlefieldNonlands.Add(card);
                break;
            case GameZone.CommandZone:
                CommandZone.Add(card);
                break;
            case GameZone.Sideboard:
                Sideboard.Add(card);
                OnPropertyChanged(nameof(HasSideboard));
                break;
        }
    }

    private void ShuffleCards<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void AddLogEntry(string message) => GameLog.Add(new GameLogEntry(message));

    private static string GetZoneDisplayName(GameZone zone) => zone switch
    {
        GameZone.Library => "Library",
        GameZone.Hand => "Hand",
        GameZone.Graveyard => "Graveyard",
        GameZone.Exile => "Exile",
        GameZone.Stack => "Stack",
        GameZone.BattlefieldLands => "Battlefield (Lands)",
        GameZone.Battlefield => "Battlefield",
        GameZone.CommandZone => "Command Zone",
        GameZone.Sideboard => "Sideboard",
        _ => zone.ToString()
    };

    private void LogZoneMove(string cardName, GameZone source, GameZone target)
    {
        AddLogEntry($"Player moves {cardName} from [{GetZoneDisplayName(source)}] to [{GetZoneDisplayName(target)}]");
    }

    /// <summary>
    /// Toggle the tap state of a battlefield card and log the action.
    /// </summary>
    public void ToggleTap(PlaytestCardViewModel card)
    {
        card.IsTapped = !card.IsTapped;
        AddLogEntry(card.IsTapped
            ? $"Player taps {card.CardName}"
            : $"Player untaps {card.CardName}");
    }

    private void WireCollectionObservers()
    {
        Graveyard.CollectionChanged += OnGraveyardCollectionChanged;
        Exile.CollectionChanged += OnExileCollectionChanged;
    }

    private void OnGraveyardCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TopGraveyardCard));
    }

    private void OnExileCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TopExileCard));
    }
}
