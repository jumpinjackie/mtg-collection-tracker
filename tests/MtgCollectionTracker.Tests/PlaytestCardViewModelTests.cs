using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for <see cref="PlaytestCardViewModel"/> covering property mapping,
/// tap/untap toggling, transform and the computed PT property.
/// </summary>
public class PlaytestCardViewModelTests
{
    private static PlaytestCardViewModel CreateCardVm()
    {
        var mockFs = new Mock<ICardImageFileSystem>();
        mockFs.Setup(f => f.TryGetStream(It.IsAny<string>(), It.IsAny<string>())).Returns((Stream?)null);
        var mockClient = new Mock<IScryfallApiClient>();
        // The db factory should never be called in these tests because test cards have no ScryfallId
        Func<StrongInject.Owned<MtgCollectionTracker.Data.CardsDbContext>> neverInvoked =
            () => throw new InvalidOperationException("DB should not be accessed in card VM tests");
        var cache = new CardImageCache(neverInvoked, mockFs.Object, mockClient.Object);
        return new PlaytestCardViewModel(cache);
    }

    private static PlaytestCard MakeModel(
        string name = "Lightning Bolt",
        bool isLand = false,
        bool isDoubleFaced = false,
        bool isToken = false,
        bool isTapped = false,
        bool isFrontFace = true,
        string? power = "3",
        string? toughness = "2",
        GameZone zone = GameZone.Library)
    {
        return new PlaytestCard
        {
            CardName = name,
            ScryfallId = null, // null so image loading short-circuits
            ScryfallIdBack = null,
            ManaCost = "{R}",
            CardType = isLand ? "Basic Land" : "Instant",
            Power = power,
            Toughness = toughness,
            OracleText = "Deal 3 damage to any target.",
            IsLand = isLand,
            IsDoubleFaced = isDoubleFaced,
            IsToken = isToken,
            IsTapped = isTapped,
            IsFrontFace = isFrontFace,
            Zone = zone,
        };
    }

    [Fact]
    public void InitializeFrom_SetsAllNonImageProperties()
    {
        var vm = CreateCardVm();
        var model = MakeModel(
            name: "Snapcaster Mage",
            isLand: false,
            isDoubleFaced: false,
            isToken: false,
            isTapped: false,
            isFrontFace: true,
            power: "2",
            toughness: "1",
            zone: GameZone.Hand);
        model.ManaCost = "{1}{U}";
        model.OracleText = "Flash. Flashback.";
        model.CardType = "Human Wizard";

        vm.InitializeFrom(model);

        Assert.Equal("Snapcaster Mage", vm.CardName);
        Assert.Equal("{1}{U}", vm.ManaCost);
        Assert.Equal("Human Wizard", vm.CardType);
        Assert.Equal("2", vm.Power);
        Assert.Equal("1", vm.Toughness);
        Assert.Equal("Flash. Flashback.", vm.OracleText);
        Assert.False(vm.IsLand);
        Assert.False(vm.IsDoubleFaced);
        Assert.False(vm.IsToken);
        Assert.False(vm.IsTapped);
        Assert.True(vm.IsFrontFace);
        Assert.Equal(GameZone.Hand, vm.Zone);
        Assert.Null(vm.ScryfallId);
        Assert.Null(vm.ScryfallIdBack);
    }

    [Fact]
    public void PT_ReturnsPowerSlashToughness_WhenBothAreSet()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(power: "4", toughness: "5"));

        Assert.Equal("4/5", vm.PT);
    }

    [Fact]
    public void PT_ReturnsNull_WhenPowerIsNull()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(power: null, toughness: "5"));

        Assert.Null(vm.PT);
    }

    [Fact]
    public void PT_ReturnsNull_WhenToughnessIsNull()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(power: "4", toughness: null));

        Assert.Null(vm.PT);
    }

    [Fact]
    public void ToggleTapped_FlipsIsTappedToTrue_WhenCurrentlyUntapped()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isTapped: false));

        vm.ToggleTapped();

        Assert.True(vm.IsTapped);
    }

    [Fact]
    public void ToggleTapped_FlipsIsTappedToFalse_WhenCurrentlyTapped()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isTapped: true));

        vm.ToggleTapped();

        Assert.False(vm.IsTapped);
    }

    [Fact]
    public void ToggleTapped_SetsRotationAngleTo90_WhenTapping()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isTapped: false));

        vm.ToggleTapped();

        Assert.Equal(90, vm.RotationAngle);
    }

    [Fact]
    public void ToggleTapped_SetsRotationAngleTo0_WhenUntapping()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isTapped: true));

        vm.ToggleTapped();

        Assert.Equal(0, vm.RotationAngle);
    }

    [Fact]
    public void Transform_FlipsToBackFace_WhenDoubleFaced()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isDoubleFaced: true, isFrontFace: true));

        vm.Transform();

        Assert.False(vm.IsFrontFace);
    }

    [Fact]
    public void Transform_FlipsToFrontFace_WhenOnBackFaceOfDoubleFacedCard()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isDoubleFaced: true, isFrontFace: false));

        vm.Transform();

        Assert.True(vm.IsFrontFace);
    }

    [Fact]
    public void Transform_DoesNothing_WhenNotDoubleFaced()
    {
        var vm = CreateCardVm();
        vm.InitializeFrom(MakeModel(isDoubleFaced: false, isFrontFace: true));

        vm.Transform();

        Assert.True(vm.IsFrontFace);
    }
}
