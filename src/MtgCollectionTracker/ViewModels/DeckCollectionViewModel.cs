using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckCollectionViewModel : RecipientViewModelBase, IViewModelWithBusyState, IRecipient<DeckCreatedMessage>, IRecipient<DeckDismantledMessage>, IRecipient<CardsSentToDeckMessage>
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;

    public DeckCollectionViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.SelectedFormats.CollectionChanged += SelectedFormats_CollectionChanged;
        this.IsActive = true;
    }

    public DeckCollectionViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service, IMessenger messenger, IScryfallApiClient scryfallApiClient)
        : base(messenger)
    {
        _vmFactory = vmFactory;
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        this.SelectedFormats.CollectionChanged += SelectedFormats_CollectionChanged;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.ResetFiltersCommand.Execute(null);
        }
        base.OnActivated();
    }

    private bool _silentSelectedFormatsUpdate = false;

    private void SelectedFormats_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_silentSelectedFormatsUpdate)
            return;

        this.RefreshDecks();
    }

    [RelayCommand]
    private void ResetFilters()
    {
        this.Formats.Clear();
        this.SelectedFormats.Clear();

        var formats = _service.GetDeckFormats();
        foreach (var fmt in formats)
        {
            this.Formats.Add(fmt);
            this.SelectedFormats.Add(fmt);
        }

        this.RefreshDecks();
    }

    public ObservableCollection<string> Formats { get; } = new();

    public ObservableCollection<string> SelectedFormats { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunAgainstSelectedDeck))]
    private bool _isBusy;

    public ObservableCollection<DeckViewModel> Decks { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunAgainstSelectedDeck))]
    private DeckViewModel? _selectedDeck;

    public bool CanRunAgainstSelectedDeck => SelectedDeck != null && !IsBusy;

    [RelayCommand]
    private void AddDeck()
    {
        Messenger.Send(new OpenDialogMessage
        {
            DrawerWidth = 400,
            ViewModel = _vmFactory.Drawer().WithContent("New Deck", _vmFactory.NewDeckOrContainer(DeckOrContainer.Deck))
        });
    }

    [RelayCommand]
    private void DismantleDeck()
    {
        if (this.SelectedDeck != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Dismantle Deck",
                    $"Are you sure you want to dismantle ({this.SelectedDeck.Name})?", 
                    async () =>
                    {
                        await _service.DismantleDeckAsync(new() { DeckId = this.SelectedDeck.DeckId });
                        this.Messenger.ToastNotify("Deck dismantled");
                        this.Messenger.Send(new DeckDismantledMessage { Id = this.SelectedDeck.DeckId, Format = this.SelectedDeck.Format });
                    })
            });
        }
    }

    [RelayCommand]
    private void RefreshDecks()
    {
        this.Decks.Clear();
        var decks = _service.GetDecks(new DeckFilterModel { Formats = this.SelectedFormats });
        foreach (var deck in decks)
        {
            this.Decks.Add(_vmFactory.Deck().WithData(deck));
        }
    }

    [RelayCommand]
    private void ViewDeckContents()
    {
        if (this.SelectedDeck != null)
        {
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 450,
                ViewModel = _vmFactory.Drawer().WithContent("Deck List", _vmFactory.DeckList().WithDeck(this.SelectedDeck.DeckId, this.SelectedDeck.Name))
            });
        }
    }

    [RelayCommand]
    private async Task ViewDeckVisually()
    {
        if (this.SelectedDeck != null)
        {
            DeckModel deck;
            using (((IViewModelWithBusyState)this).StartBusyState())
            {
                deck = await _service.GetDeckAsync(this.SelectedDeck.DeckId, _scryfallApiClient, CancellationToken.None);
            }
            Messenger.Send(new OpenDialogMessage
            {
                DrawerWidth = 1280,
                ViewModel = _vmFactory.Drawer().WithContent("Deck: " + deck.Name, _vmFactory.DeckVisual().WithDeck(deck))
            });
        }
    }

    [RelayCommand]
    private void CheckDeckLegality()
    {
        Messenger.ToastNotify("Feature not implemented yet");
    }

    void IRecipient<DeckCreatedMessage>.Receive(DeckCreatedMessage message)
    {
        this.Decks.Add(_vmFactory.Deck().WithData(message.Deck));

        if (!string.IsNullOrEmpty(message.Deck.Format) && !this.Formats.Contains(message.Deck.Format))
        {
            this.Formats.Add(message.Deck.Format);
            try
            {
                _silentSelectedFormatsUpdate = true;
                this.SelectedFormats.Add(message.Deck.Format);
            }
            finally
            {
                _silentSelectedFormatsUpdate = false;
            }
        }
    }

    void IRecipient<DeckDismantledMessage>.Receive(DeckDismantledMessage message)
    {
        var item = this.Decks.FirstOrDefault(d => d.DeckId == message.Id);
        if (item != null)
            this.Decks.Remove(item);

        // If this was the last deck in this format, remove format from filter list
        if (!_service.HasOtherDecksInFormat(message.Format))
        {
            this.Formats.Remove(message.Format);
            try
            {
                _silentSelectedFormatsUpdate = true;
                this.SelectedFormats.Remove(message.Format);
            }
            finally
            {
                _silentSelectedFormatsUpdate = false;
            }
        }
    }

    void IRecipient<CardsSentToDeckMessage>.Receive(CardsSentToDeckMessage message)
    {
        // Update totals of given deck
        var deck = this.Decks.FirstOrDefault(d => d.DeckId == message.DeckId);
        if (deck != null)
        {
            var sum = _service.GetDecks(new() { Formats = [], Ids = [deck.DeckId] });
            foreach (var s  in sum)
            {
                if (deck.DeckId == s.Id)
                    deck.WithData(s);
            }
        }
    }
}
