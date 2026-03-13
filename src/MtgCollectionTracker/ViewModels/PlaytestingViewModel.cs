using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using ScryfallApi.Client;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// Top-level ViewModel for the Playtesting tab, managing deck selection and game initialization
/// </summary>
public partial class PlaytestingViewModel : RecipientViewModelBase, IViewModelWithBusyState
{
    private readonly ICollectionTrackingService _service;
    private readonly IScryfallApiClient _scryfallClient;
    private readonly Func<PlaytestGameStateViewModel> _gameStateFactory;

    public PlaytestingViewModel(
        ICollectionTrackingService service,
        IScryfallApiClient scryfallClient,
        Func<PlaytestGameStateViewModel> gameStateFactory,
        IMessenger messenger
    )
        : base(messenger)
    {
        _service = service;
        _scryfallClient = scryfallClient;
        _gameStateFactory = gameStateFactory;
        IsActive = true;
    }

    public PlaytestingViewModel()
        : base(WeakReferenceMessenger.Default)
    {
        ThrowIfNotDesignMode();
        _service = null!;
        _scryfallClient = null!;
        _gameStateFactory = null!;
    }

    IMessenger IViewModelWithBusyState.Messenger => this.Messenger;

    public ObservableCollection<DeckSummaryModel> AvailableDecks { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeckSelected))]
    [NotifyCanExecuteChangedFor(nameof(BeginPlaytestCommand))]
    private DeckSummaryModel? _selectedDeck;

    [ObservableProperty]
    private bool _isInGame = false;

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private PlaytestGameStateViewModel? _gameState;

    public bool IsDeckSelected => SelectedDeck != null;

    /// <summary>
    /// Load available decks when the view is activated
    /// </summary>
    protected override void OnActivated()
    {
        base.OnActivated();
        // Auto-load decks on first activation if list is empty
        if (AvailableDecks.Count == 0)
        {
            RefreshDecks();
        }
    }

    /// <summary>
    /// Refresh the list of available decks
    /// </summary>
    [RelayCommand]
    private void RefreshDecks()
    {
        AvailableDecks.Clear();
        var decks = _service.GetDecks(null);
        foreach (var deck in decks.OrderBy(d => d.Name))
        {
            AvailableDecks.Add(deck);
        }
    }

    /// <summary>
    /// Begin playtesting with the selected deck
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsDeckSelected))]
    private async Task BeginPlaytest()
    {
        if (SelectedDeck == null)
            return;

        IsBusy = true;
        try
        {
            // Load the full deck
            var deck = await _service.GetDeckAsync(
                SelectedDeck.Id,
                _scryfallClient,
                CancellationToken.None
            );

            if (deck == null)
            {
                Messenger.Send(
                    new NotificationMessage
                    {
                        Content = "Failed to load deck",
                        Type = Avalonia.Controls.Notifications.NotificationType.Error,
                    }
                );
                return;
            }

            // Create game state and initialize with deck
            GameState = _gameStateFactory();
            await GameState.InitializeWithDeck(deck, _service);

            // Switch to game view
            IsInGame = true;
        }
        catch (Exception ex)
        {
            Messenger.Send(
                new NotificationMessage
                {
                    Content = $"Error loading deck: {ex.Message}",
                    Type = Avalonia.Controls.Notifications.NotificationType.Error,
                }
            );
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Return to deck selection view
    /// </summary>
    [RelayCommand]
    private void SelectDeck()
    {
        IsInGame = false;
        GameState = null;
        SelectedDeck = null;
    }
}
