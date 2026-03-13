using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckViewModel : ViewModelBase
{
    readonly ICollectionTrackingService? _service;

    public DeckViewModel()
    {
        this.Name = "My Deck";
        this.Format = "Legacy";
        this.Maindeck = "MD: 60";
        this.Sideboard = "SB: 15";
    }

    public DeckViewModel(ICollectionTrackingService service)
    {
        _service = service;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBanner))]
    private Task<Bitmap?>? _banner;

    public bool HasBanner => this.Banner != null;

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
        this.Banner = (deck.BannerScryfallId != null && _service != null)
            ? LoadBannerImageAsync(deck.BannerScryfallId)
            : null;
        return this;
    }

    private async Task<Bitmap?> LoadBannerImageAsync(string scryfallId)
    {
        using var stream = await _service!.GetSmallFrontFaceImageAsync(scryfallId);
        if (stream != null)
            return new Bitmap(stream);
        return null;
    }
}
