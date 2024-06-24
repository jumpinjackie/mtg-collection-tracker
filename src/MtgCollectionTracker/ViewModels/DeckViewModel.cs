using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Stubs;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckViewModel : ViewModelBase
{
    readonly IViewModelFactory _vmFactory;

    public DeckViewModel()
    {
        base.ThrowIfNotDesignMode();
        _vmFactory = new StubViewModelFactory();
        this.Name = "My Deck";
        this.Format = "Legacy";
        this.Maindeck = "MD: 60";
        this.Sideboard = "SB: 15";
    }

    public DeckViewModel(IViewModelFactory vmFactory)
    {
        _vmFactory = vmFactory;
        this.Name = "My Deck";
        this.Maindeck = string.Empty;
        this.Sideboard = string.Empty;
    }

    public int DeckId { get; private set; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _format;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContainer))]
    private string? _containerName;

    [ObservableProperty]
    private string _maindeck;

    [ObservableProperty]
    private string _sideboard;

    public bool HasContainer => !string.IsNullOrEmpty(this.ContainerName);

    public DeckViewModel WithData(DeckSummaryModel deck)
    {
        this.DeckId = deck.Id;
        this.Format = deck.Format ?? "Unknown Format";
        this.Name = deck.Name;
        this.ContainerName = deck.ContainerName;
        this.Maindeck = $"MD: {deck.MaindeckTotal}";
        this.Sideboard = $"SB: {deck.SideboardTotal}";
        return this;
    }
}
