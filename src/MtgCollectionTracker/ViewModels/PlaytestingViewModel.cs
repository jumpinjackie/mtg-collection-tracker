using System;
using System.Collections.Generic;
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
    private readonly List<DeckSummaryModel> _allDecks = new();

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
    private string _deckSearchText = string.Empty;

    partial void OnDeckSearchTextChanged(string value)
    {
        ApplyDeckFilter();
    }

    [ObservableProperty]
    private bool _isInGame = false;

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private PlaytestGameStateViewModel? _gameState;

    private PlaytestGameStateViewModel? _subscribedGameState;

    partial void OnGameStateChanged(PlaytestGameStateViewModel? value)
    {
        if (_subscribedGameState is not null)
            _subscribedGameState.PropertyChanged -= ForwardGameStatePropertyChanged;
        _subscribedGameState = value;
        if (value is not null)
            value.PropertyChanged += ForwardGameStatePropertyChanged;
        OnPropertyChanged(nameof(DetailsImageMaxHeight));
    }

    private void ForwardGameStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaytestGameStateViewModel.DetailsImageMaxHeight))
            OnPropertyChanged(nameof(DetailsImageMaxHeight));
    }

    /// <summary>Proxy for <see cref="PlaytestGameStateViewModel.DetailsImageMaxHeight"/>, safe to bind even when GameState is null.</summary>
    public double DetailsImageMaxHeight => GameState?.DetailsImageMaxHeight ?? (250 * 1.25);

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
            _ = RefreshDecksAsync();
        }
    }

    /// <summary>
    /// Refresh the list of available decks
    /// </summary>
    [RelayCommand]
    private async Task RefreshDecksAsync()
    {
        _allDecks.Clear();
        _allDecks.AddRange((await _service.GetDecksAsync(null, CancellationToken.None)).OrderBy(d => d.Name));
        ApplyDeckFilter();
    }

    private void ApplyDeckFilter()
    {
        var search = this.DeckSearchText?.Trim();
        var filtered = string.IsNullOrWhiteSpace(search)
            ? _allDecks
            : _allDecks.Where(d =>
                d.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (d.Format?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || d.DeckName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        AvailableDecks.Clear();
        foreach (var deck in filtered)
        {
            AvailableDecks.Add(deck);
        }

        if (this.SelectedDeck != null && !AvailableDecks.Any(d => d.Id == this.SelectedDeck.Id))
        {
            this.SelectedDeck = null;
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
