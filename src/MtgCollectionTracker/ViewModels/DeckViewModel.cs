using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckViewModel : ViewModelBase
{
    public DeckViewModel()
    {
        this.Name = "My Deck";
        this.Format = "Legacy";
        this.Maindeck = "MD: 60";
        this.Sideboard = "SB: 15";
    }

    public int DeckId { get; private set; }

    public string DeckName { get; private set; }

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
        this.DeckName = deck.DeckName;
        this.Name = deck.Name;
        this.ContainerName = deck.ContainerName;
        this.Maindeck = $"MD: {deck.MaindeckTotal}";
        this.Sideboard = $"SB: {deck.SideboardTotal}";
        return this;
    }
}
