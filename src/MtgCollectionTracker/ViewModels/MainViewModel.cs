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
    }

    public MainViewModel(IViewModelFactory vmFactory)
    {
        this.Cards = vmFactory.Cards();
    }

    public CardsViewModel Cards { get; }
}