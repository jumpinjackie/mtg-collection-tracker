using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckCollectionViewModel : RecipientViewModelBase, IRecipient<DeckCreatedMessage>, IRecipient<DeckDismantledMessage>
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public DeckCollectionViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
    }

    public DeckCollectionViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service, IMessenger messenger)
        : base(messenger)
    {
        _vmFactory = vmFactory;
        _service = service;
        this.IsActive = true;
    }

    protected override void OnActivated()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            this.Decks.Clear();
            var decks = _service.GetDecks(null);
            foreach (var deck in decks)
            {
                this.Decks.Add(_vmFactory.Deck().WithData(deck));
            }
        }
        base.OnActivated();
    }

    public ObservableCollection<DeckViewModel> Decks { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDeck))]
    private DeckViewModel? _selectedDeck;

    public bool HasSelectedDeck => this.SelectedDeck != null;

    [RelayCommand]
    private void AddDeck()
    {
        Messenger.Send(new OpenDrawerMessage
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
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 800,
                ViewModel = _vmFactory.Drawer().WithConfirmation(
                    "Dismantle Deck",
                    $"Are you sure you want to dismantle ({this.SelectedDeck.Name})?", 
                    async () =>
                    {
                        await _service.DismantleDeckAsync(new() { DeckId = this.SelectedDeck.DeckId });
                        this.Messenger.ToastNotify("Deck dismantled");
                        this.Messenger.Send(new DeckDismantledMessage { Id = this.SelectedDeck.DeckId });
                    })
            });
        }
    }

    [RelayCommand]
    private void CanIBuildThisDeck()
    {

    }

    [RelayCommand]
    private void ViewDeckContents()
    {
        if (this.SelectedDeck != null)
        {
            Messenger.Send(new OpenDrawerMessage
            {
                DrawerWidth = 450,
                ViewModel = _vmFactory.Drawer().WithContent("Deck List", _vmFactory.DeckList().WithDeck(this.SelectedDeck.DeckId, this.SelectedDeck.Name))
            });
        }
    }

    [RelayCommand]
    private void CheckDeckLegality()
    {

    }

    void IRecipient<DeckCreatedMessage>.Receive(DeckCreatedMessage message)
    {
        this.Decks.Add(_vmFactory.Deck().WithData(message.Deck));
    }

    void IRecipient<DeckDismantledMessage>.Receive(DeckDismantledMessage message)
    {
        var item = this.Decks.FirstOrDefault(d => d.DeckId == message.Id);
        if (item != null)
            this.Decks.Remove(item);
    }
}
