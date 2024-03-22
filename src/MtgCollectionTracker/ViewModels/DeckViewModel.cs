using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System.Reflection;

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

    [ObservableProperty]
    private string _maindeck;

    [ObservableProperty]
    private string _sideboard;

    public DeckViewModel WithData(DeckSummaryModel deck)
    {
        this.Name = deck.Format?.Length > 0
            ? $"[{deck.Format}] {deck.Name}"
            : deck.Name;
        this.ContainerName = deck.ContainerName;
        this.Maindeck = $"MD: {deck.MaindeckTotal}";
        this.Sideboard = $"SB: {deck.SideboardTotal}";
        return this;
    }
}
