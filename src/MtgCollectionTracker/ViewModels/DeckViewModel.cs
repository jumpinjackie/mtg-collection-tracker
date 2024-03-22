using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckViewModel : ViewModelBase
{
    readonly IViewModelFactory _vmFactory;

    public DeckViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        this.Name = "[Legacy] My Deck";
    }

    public DeckViewModel(IViewModelFactory vmFactory)
    {
        _vmFactory = vmFactory;
        this.Name = "My Deck";
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _containerName;

    public DeckViewModel WithData(DeckSummaryModel deck)
    {
        this.Name = deck.Format?.Length > 0
            ? $"[{deck.Format}] {deck.Name}"
            : deck.Name;
        this.ContainerName = deck.ContainerName;
        return this;
    }
}
