using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for the <see cref="CardListPrinter"/> utility, covering proxy-set detection and deck/container printing.
/// </summary>
public class CardListPrinterTests
{
    private sealed class FakeSlot(string cardName, string edition, int quantity, bool isLand = false, bool isSideboard = false) : IDeckPrintableSlot
    {
        public int Quantity { get; } = quantity;
        public string CardName { get; } = cardName;
        public string Edition { get; } = edition;
        public bool IsLand { get; } = isLand;
        public bool IsSideboard { get; } = isSideboard;
    }

    [Theory]
    [InlineData("PROXY")]
    [InlineData("PTC")]
    [InlineData("WC97")]
    [InlineData("WC98")]
    [InlineData("WC99")]
    [InlineData("WC00")]
    [InlineData("WC01")]
    [InlineData("WC02")]
    [InlineData("WC03")]
    [InlineData("WC04")]
    [InlineData("CED")]
    [InlineData("CEI")]
    [InlineData("30A")]
    public void IsProxyEdition_ReturnsTrue_ForKnownProxySets(string edition)
    {
        Assert.True(CardListPrinter.IsProxyEdition(edition));
    }

    [Theory]
    [InlineData("M10")]
    [InlineData("RAV")]
    [InlineData("ZNR")]
    [InlineData("MID")]
    [InlineData("")]
    public void IsProxyEdition_ReturnsFalse_ForRegularEditions(string edition)
    {
        Assert.False(CardListPrinter.IsProxyEdition(edition));
    }

    [Fact]
    public void PrintDeck_IncludesDeckNameAndFormat()
    {
        var lines = new List<string>();
        var cards = new List<FakeSlot>
        {
            new("Lightning Bolt", "M10", 4),
            new("Dark Ritual", "LEA", 4),
            new("Swamp", "M10", 12, isLand: true),
            new("Mountain", "M10", 8, isLand: true),
            new("Duress", "M10", 4, isSideboard: true),
            new("Thoughtseize", "LRW", 4, isSideboard: true),
            new("Pyroblast", "ICE", 4, isSideboard: true),
            new("Surgical Extraction", "NPH", 3, isSideboard: true),
        };

        CardListPrinter.PrintDeck("Test Deck", "Legacy", cards, lines.Add, new DeckPrintOptions(false));

        Assert.Contains(lines, l => l.Contains("Test Deck"));
        Assert.Contains(lines, l => l.Contains("Legacy"));
        Assert.Contains(lines, l => l.Contains("Lightning Bolt"));
        Assert.Contains(lines, l => l.Contains("Swamp"));
        Assert.Contains(lines, l => l.Contains("Duress"));
    }

    [Fact]
    public void PrintDeck_ReportsNoSideboard_WhenEmpty()
    {
        var lines = new List<string>();
        var cards = new List<FakeSlot>
        {
            new("Plains", "M10", 20, isLand: true),
            new("Serra Angel", "M10", 40),
        };

        CardListPrinter.PrintDeck("Test Deck", null, cards, lines.Add, new DeckPrintOptions(false));

        Assert.Contains(lines, l => l.Contains("WARNING") && l.Contains("sideboard"));
    }

    [Fact]
    public void PrintDeck_ReportsSideboardShortfall_WhenUnderLimit()
    {
        var lines = new List<string>();
        var cards = new List<FakeSlot>
        {
            new("Serra Angel", "M10", 60),
            new("Counterspell", "M10", 5, isSideboard: true),
        };

        CardListPrinter.PrintDeck("Test Deck", null, cards, lines.Add, new DeckPrintOptions(false));

        Assert.Contains(lines, l => l.Contains("Sideboard") && l.Contains("short"));
    }

    [Fact]
    public void PrintContainer_IncludesNameAndDescription()
    {
        var lines = new List<string>();
        var cards = new List<FakeSlot>
        {
            new("Birds of Paradise", "RAV", 4),
            new("Force of Will", "ALL", 2),
        };

        CardListPrinter.PrintContainer("My Binder", "Vintage staples", cards, lines.Add, new ContainerPrintOptions(false));

        Assert.Contains(lines, l => l.Contains("My Binder"));
        Assert.Contains(lines, l => l.Contains("Vintage staples"));
        Assert.Contains(lines, l => l.Contains("Birds of Paradise"));
        Assert.Contains(lines, l => l.Contains("Force of Will"));
    }

    [Fact]
    public void PrintContainer_ReportsProxies_WhenEnabled()
    {
        var lines = new List<string>();
        var cards = new List<FakeSlot>
        {
            new("Black Lotus", "PROXY", 1),
            new("Plains", "M10", 10, isLand: true),
        };

        CardListPrinter.PrintContainer("Proxies", null, cards, lines.Add, new ContainerPrintOptions(true));

        Assert.Contains(lines, l => l.Contains("proxies"));
    }
}
