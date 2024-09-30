using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services.Stubs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace MtgCollectionTracker.ViewModels;

public enum DeckViewMode
{
    /// <summary>
    /// View the decklist in text mode
    /// </summary>
    Text,
    /// <summary>
    /// View the decklist as a table, by SKUs
    /// </summary>
    TableBySku,
    /// <summary>
    /// View the decklist visually, by SKUs
    /// </summary>
    VisualBySku,
    /// <summary>
    /// View the decklist as a table, by card name
    /// </summary>
    TableByCardName,
    /// <summary>
    /// View the decklist visually, by card name
    /// </summary>
    VisualByCardName
}

public partial class DeckDetailsViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;

    [ObservableProperty]
    private string _name;

    public DeckDetailsViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
        this.Name = "Test Deck";
    }

    public DeckDetailsViewModel(ICollectionTrackingService service)
    {
        _service = service;
        this.IsActive = true;
    }

    [ObservableProperty]
    private DeckViewMode _mode = DeckViewMode.Text;

    // For text view

    [ObservableProperty]
    private string _deckListText;

    // For visual view (by SKU)

    public ObservableCollection<CardVisualViewModel> MainDeckBySku { get; } = new();

    public ObservableCollection<CardVisualViewModel> SideboardBySku { get; } = new();

    // For visual view (by card name)

    public ObservableCollection<CardVisualViewModel> MainDeckByCardName { get; } = new();

    public ObservableCollection<CardVisualViewModel> SideboardByCardName { get; } = new();
    
    // Misc properties

    [ObservableProperty]
    private int _mainDeckSize;

    [ObservableProperty]
    private int _sideboardSize;

    public record CardSlotImpl(int Quantity, string CardName, string Edition, bool IsLand, bool IsSideboard) : IDeckPrintableSlot;

    [RelayCommand]
    private void PrintDeck(bool reportProxyUsage)
    {
        var text = new StringBuilder();
        DeckPrinter.Print(_origDeck.Name, _origDeck.Format, _cards, s => text.AppendLine(s), reportProxyUsage);
        this.DeckListText = text.ToString();
    }

    private List<IDeckPrintableSlot> _cards = new();

    private DeckModel _origDeck;

    public DeckDetailsViewModel WithDeck(DeckModel deck)
    {
        int mdTotal = 0;
        int sbTotal = 0;
        this.Name = deck.Name;

        _origDeck = deck;

        // Setup data by SKU
        {
            var md = new List<CardVisualViewModel>();
            foreach (var grp in deck.MainDeck.GroupBy(c => c.SkuId))
            {
                var card = grp.First();

                var cm = new CardVisualViewModel { IsGrouped = true, Quantity = grp.Count(), CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, Edition = card.Edition, IsProxy = DeckPrinter.IsProxyEdition(card.Edition), CardImage = TryGetFrontFaceImage(card) };
                md.Add(cm);
                mdTotal += grp.Count();
            }
            // Non-lands before lands
            foreach (var c in md.OrderBy(m => m.IsLand).ThenBy(m => m.Type))
                this.MainDeckBySku.Add(c);

            foreach (var grp in deck.Sideboard.GroupBy(c => c.SkuId))
            {
                var card = grp.First();
                this.SideboardBySku.Add(new CardVisualViewModel { IsGrouped = true, Quantity = grp.Count(), CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, Edition = card.Edition, IsProxy = DeckPrinter.IsProxyEdition(card.Edition), CardImage = TryGetFrontFaceImage(card), IsSideboard = true });
                sbTotal += grp.Count();
            }
        }

        // Setup data by card name
        {
            var md = new List<CardVisualViewModel>();
            foreach (var grp in deck.MainDeck.GroupBy(c => c.CardName))
            {
                var card = grp.First();

                var cm = new CardVisualViewModel { IsGrouped = true, Quantity = grp.Count(), CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, Edition = card.Edition, IsProxy = DeckPrinter.IsProxyEdition(card.Edition), CardImage = TryGetFrontFaceImage(card) };
                md.Add(cm);
            }
            // Non-lands before lands
            foreach (var c in md.OrderBy(m => m.IsLand).ThenBy(m => m.Type))
                this.MainDeckByCardName.Add(c);

            foreach (var grp in deck.Sideboard.GroupBy(c => c.CardName))
            {
                var card = grp.First();
                this.SideboardByCardName.Add(new CardVisualViewModel { IsGrouped = true, Quantity = grp.Count(), CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, Edition = card.Edition, IsProxy = DeckPrinter.IsProxyEdition(card.Edition), CardImage = TryGetFrontFaceImage(card), IsSideboard = true });
            }
        }

        // The slots by card name are printable
        _cards.AddRange(this.MainDeckByCardName);
        _cards.AddRange(this.SideboardByCardName);

        // Default to without proxy stats
        this.PrintDeck(false);

        this.MainDeckSize = mdTotal;
        this.SideboardSize = sbTotal;

        return this;

        static Bitmap? TryGetFrontFaceImage(DeckCardModel card)
        {
            if (card.FrontFaceImage != null)
            {
                using var ms = new MemoryStream(card.FrontFaceImage);
                return new Bitmap(ms);
            }
            return null;
        }
    }
}
