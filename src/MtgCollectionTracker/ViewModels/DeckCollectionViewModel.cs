using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckCollectionViewModel : RecipientViewModelBase
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;
    readonly IMessenger _messenger;

    public DeckCollectionViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
        _messenger = WeakReferenceMessenger.Default;
        this.IsActive = true;
    }

    public DeckCollectionViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service, IMessenger messenger)
    {
        _vmFactory = vmFactory;
        _service = service;
        _messenger = messenger;
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
    }

    public ObservableCollection<DeckViewModel> Decks { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDeck))]
    private DeckViewModel? _selectedDeck;

    public bool HasSelectedDeck => this.SelectedDeck != null;

    [RelayCommand]
    private void AddDeck()
    {

    }

    [RelayCommand]
    private void DismantleDeck()
    {

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
            _messenger.Send(new OpenDrawerMessage
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
}
