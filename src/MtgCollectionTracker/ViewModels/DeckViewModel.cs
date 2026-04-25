using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using System;
using System.Diagnostics;
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
    private Task<Bitmap?> _banner = Task.FromResult<Bitmap?>(null);

    [ObservableProperty]
    private bool _hasBanner;

    public bool HasContainer => !string.IsNullOrEmpty(this.ContainerName);

    /// <summary>
    /// Whether this is a Commander deck
    /// </summary>
    public bool IsCommander { get; private set; }

    /// <summary>
    /// The name of the commander card (null if not a commander deck or no commander assigned)
    /// </summary>
    public string? CommanderName { get; private set; }

    /// <summary>
    /// The commander validation status (null if not a commander deck)
    /// </summary>
    public bool? IsCommanderValid { get; private set; }

    /// <summary>
    /// Tooltip content for the commander badge.
    /// </summary>
    public string CommanderTooltip { get; private set; } = string.Empty;

    public bool HasCommanderValid => IsCommander && IsCommanderValid == true;

    public bool HasCommanderInvalid => IsCommander && IsCommanderValid == false;

    public DeckViewModel WithData(DeckSummaryModel deck)
    {
        this.DeckId = deck.Id;
        this.Format = deck.Format ?? "Unknown Format";
        this.DeckName = deck.DeckName;
        this.Name = deck.Name;
        this.ContainerName = deck.ContainerName;
        this.Maindeck = $"MD: {deck.MaindeckTotal}";
        this.Sideboard = $"SB: {deck.SideboardTotal}";
        this.HasBanner = deck.BannerCardId != null && _service != null;
        this.Banner = this.HasBanner
            ? LoadBannerImageAsync(deck.BannerCardId!.Value)
            : Task.FromResult<Bitmap?>(null);
        this.IsCommander = deck.IsCommander;
        this.CommanderName = deck.CommanderName;
        this.IsCommanderValid = deck.IsCommanderValid;
        this.CommanderTooltip = deck.CommanderValidationMessage
            ?? deck.CommanderName
            ?? this.Name;

        // These are plain properties (not ObservableProperty), so notify bindings explicitly
        // when a deck summary update arrives for an existing tile view model.
        OnPropertyChanged(nameof(IsCommander));
        OnPropertyChanged(nameof(CommanderName));
        OnPropertyChanged(nameof(IsCommanderValid));
        OnPropertyChanged(nameof(CommanderTooltip));
        OnPropertyChanged(nameof(HasCommanderValid));
        OnPropertyChanged(nameof(HasCommanderInvalid));

        return this;
    }

    private async Task<Bitmap?> LoadBannerImageAsync(Guid cardSkuId)
    {
        try
        {
            using var stream = await _service!.GetSmallFrontFaceImageAsync(cardSkuId, System.Threading.CancellationToken.None);
            if (stream != null)
                return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            // Image data could not be decoded (e.g. corrupted cache entry).
            Debug.WriteLine($"[DeckViewModel] Failed to decode banner image for sku {cardSkuId}: {ex.Message}");
        }

        // Fall back to the stock deckbox icon by clearing the banner flag.
        HasBanner = false;
        return null;
    }
}
