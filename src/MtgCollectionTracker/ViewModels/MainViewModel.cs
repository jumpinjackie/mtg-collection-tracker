using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;

namespace MtgCollectionTracker.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        base.ThrowIfNotDesignMode();
        var vmFactory = new StubViewModelFactory();
        this.Cards = vmFactory.Cards();
        this.Decks = vmFactory.Decks();
        this.Containers = vmFactory.Containers();
    }

    public MainViewModel(IViewModelFactory vmFactory)
    {
        this.Cards = vmFactory.Cards();
        this.Decks = vmFactory.Decks();
        this.Containers = vmFactory.Containers();
    }

    public CardsViewModel Cards { get; }

    public DeckCollectionViewModel Decks { get; }

    public ContainerSetViewModel Containers { get; }
}