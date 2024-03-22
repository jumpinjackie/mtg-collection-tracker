using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.ObjectModel;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckCollectionViewModel : ViewModelBase
{
    readonly IViewModelFactory _vmFactory;
    readonly ICollectionTrackingService _service;

    public DeckCollectionViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        _service = new StubCollectionTrackingService();
    }

    public DeckCollectionViewModel(IViewModelFactory vmFactory, ICollectionTrackingService service)
    {
        _vmFactory = vmFactory;
        _service = service;
    }

    internal void LoadDecks()
    {
        this.Decks.Clear();
        var decks = _service.GetDecks(null);
        foreach (var deck in decks)
        {
            this.Decks.Add(_vmFactory.Deck().WithData(deck));
        }
    }

    public ObservableCollection<DeckViewModel> Decks { get; } = new();

    [ObservableProperty]
    private DeckViewModel? _selectedDeck;
}
