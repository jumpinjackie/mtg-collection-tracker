using CommunityToolkit.Mvvm.Messaging;
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

/// <summary>
/// Tests for <see cref="PlaytestGameStateViewModel"/> covering counter management,
/// zone-movement operations, phase progression and game reset logic.
/// </summary>
public class PlaytestGameStateViewModelTests
{
    private static PlaytestCardViewModel CreateCardVm()
    {
        var mockFs = new Mock<ICardImageFileSystem>();
        mockFs.Setup(f => f.TryGetStream(It.IsAny<string>(), It.IsAny<string>())).Returns((Stream?)null);
        var mockClient = new Mock<IScryfallApiClient>();
        Func<StrongInject.Owned<MtgCollectionTracker.Data.CardsDbContext>> neverInvoked =
            () => throw new InvalidOperationException("DB should not be accessed in card VM tests");
        var cache = new CardImageCache(neverInvoked, mockFs.Object, mockClient.Object);
        return new PlaytestCardViewModel(cache);
    }

    private PlaytestGameStateViewModel CreateGameState()
    {
        var messenger = new WeakReferenceMessenger();
        return new PlaytestGameStateViewModel(messenger, CreateCardVm);
    }

    /// <summary>
    /// Creates a <see cref="PlaytestCardViewModel"/> initialized from a model and places it directly in the Hand zone.
    /// </summary>
    private PlaytestCardViewModel MakeCardInHand(PlaytestGameStateViewModel game, bool isLand = false, string? cardType = null)
    {
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = isLand ? "Forest" : "Lightning Bolt",
            IsLand = isLand,
            CardType = cardType ?? (isLand ? "Basic Land — Forest" : "Instant"),
            Zone = GameZone.Hand,
            IsFrontFace = true,
        });
        game.Hand.Add(card);
        return card;
    }

    /// <summary>
    /// Creates a <see cref="PlaytestCardViewModel"/> initialized from a model and places it directly in the Library.
    /// </summary>
    private PlaytestCardViewModel AddCardToLibrary(PlaytestGameStateViewModel game, bool isLand = false)
    {
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = isLand ? "Plains" : "Counterspell",
            IsLand = isLand,
            CardType = isLand ? "Basic Land — Plains" : "Instant",
            Zone = GameZone.Library,
            IsFrontFace = true,
        });
        game.Library.Add(card);
        return card;
    }

    // ─── Counter management ───────────────────────────────────────────────────

    [Fact]
    public void IncrementLifeCommand_IncreasesLifeTotalByOne()
    {
        var game = CreateGameState();

        game.IncrementLifeCommand.Execute(null);

        Assert.Equal(21, game.LifeTotal);
    }

    [Fact]
    public void DecrementLifeCommand_DecreasesLifeTotalByOne()
    {
        var game = CreateGameState();

        game.DecrementLifeCommand.Execute(null);

        Assert.Equal(19, game.LifeTotal);
    }

    [Fact]
    public void DecrementWhiteManaCommand_NeverGoesBelowZero()
    {
        var game = CreateGameState();
        Assert.Equal(0, game.WhiteMana);

        game.DecrementWhiteManaCommand.Execute(null);

        Assert.Equal(0, game.WhiteMana);
    }

    [Fact]
    public void IncrementAndDecrement_AllManaTypes_WorkCorrectly()
    {
        var game = CreateGameState();

        game.IncrementBlueManaCommand.Execute(null);
        game.IncrementBlackManaCommand.Execute(null);
        game.IncrementRedManaCommand.Execute(null);
        game.IncrementGreenManaCommand.Execute(null);
        game.IncrementColorlessManaCommand.Execute(null);

        Assert.Equal(1, game.BlueMana);
        Assert.Equal(1, game.BlackMana);
        Assert.Equal(1, game.RedMana);
        Assert.Equal(1, game.GreenMana);
        Assert.Equal(1, game.ColorlessMana);

        game.DecrementBlueManaCommand.Execute(null);
        game.DecrementBlackManaCommand.Execute(null);
        game.DecrementRedManaCommand.Execute(null);
        game.DecrementGreenManaCommand.Execute(null);
        game.DecrementColorlessManaCommand.Execute(null);

        Assert.Equal(0, game.BlueMana);
        Assert.Equal(0, game.BlackMana);
        Assert.Equal(0, game.RedMana);
        Assert.Equal(0, game.GreenMana);
        Assert.Equal(0, game.ColorlessMana);
    }

    [Fact]
    public void DecrementMana_WhenAlreadyZero_RemainsZero()
    {
        var game = CreateGameState();

        game.DecrementBlueManaCommand.Execute(null);
        game.DecrementBlackManaCommand.Execute(null);
        game.DecrementRedManaCommand.Execute(null);
        game.DecrementGreenManaCommand.Execute(null);
        game.DecrementColorlessManaCommand.Execute(null);

        Assert.Equal(0, game.BlueMana);
        Assert.Equal(0, game.BlackMana);
        Assert.Equal(0, game.RedMana);
        Assert.Equal(0, game.GreenMana);
        Assert.Equal(0, game.ColorlessMana);
    }

    [Fact]
    public void IncrementStormCommand_IncreasesStormCount()
    {
        var game = CreateGameState();

        game.IncrementStormCommand.Execute(null);
        game.IncrementStormCommand.Execute(null);

        Assert.Equal(2, game.StormCount);
    }

    [Fact]
    public void DecrementStormCommand_NeverGoesBelowZero()
    {
        var game = CreateGameState();

        game.DecrementStormCommand.Execute(null);

        Assert.Equal(0, game.StormCount);
    }

    [Fact]
    public void IncrementEnergyCommand_IncreasesEnergyCount()
    {
        var game = CreateGameState();

        game.IncrementEnergyCommand.Execute(null);

        Assert.Equal(1, game.EnergyCount);
    }

    [Fact]
    public void DecrementEnergyCommand_NeverGoesBelowZero()
    {
        var game = CreateGameState();

        game.DecrementEnergyCommand.Execute(null);

        Assert.Equal(0, game.EnergyCount);
    }

    // ─── Card-drawing operations ──────────────────────────────────────────────

    [Fact]
    public void DrawCard_MovesTopCardFromLibraryToHand()
    {
        var game = CreateGameState();
        var card = AddCardToLibrary(game);

        game.DrawCardCommand.Execute(null);

        Assert.Empty(game.Library);
        Assert.Single(game.Hand);
        Assert.Equal(GameZone.Hand, game.Hand[0].Zone);
        Assert.Same(card, game.Hand[0]);
    }

    [Fact]
    public void DrawCard_UpdatesLibraryCount()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        AddCardToLibrary(game);

        game.DrawCardCommand.Execute(null);

        Assert.Equal(1, game.LibraryCount);
    }

    [Fact]
    public void DrawCard_DoesNothing_WhenLibraryIsEmpty()
    {
        var game = CreateGameState();

        game.DrawCardCommand.Execute(null);

        Assert.Empty(game.Hand);
        Assert.Equal(0, game.LibraryCount);
    }

    [Fact]
    public void DrawCards_DrawsExactSpecifiedCount()
    {
        var game = CreateGameState();
        for (int i = 0; i < 10; i++) AddCardToLibrary(game);

        game.DrawCards(3);

        Assert.Equal(3, game.Hand.Count);
        Assert.Equal(7, game.LibraryCount);
    }

    [Fact]
    public void DrawCards_DrawsOnlyAvailable_WhenCountExceedsLibrarySize()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        AddCardToLibrary(game);

        game.DrawCards(5);

        Assert.Equal(2, game.Hand.Count);
        Assert.Equal(0, game.LibraryCount);
    }

    // ─── Mill and exile from library ──────────────────────────────────────────

    [Fact]
    public void MillCards_MovesCardsFromLibraryToTopOfGraveyard()
    {
        var game = CreateGameState();
        var card1 = AddCardToLibrary(game);
        var card2 = AddCardToLibrary(game);

        game.MillCards(2);

        Assert.Empty(game.Library);
        Assert.Equal(2, game.Graveyard.Count);
        Assert.Equal(GameZone.Graveyard, card1.Zone);
        Assert.Equal(GameZone.Graveyard, card2.Zone);
        // First card milled becomes top of graveyard (insert at 0)
        Assert.Same(card2, game.Graveyard[0]);
        Assert.Same(card1, game.Graveyard[1]);
    }

    [Fact]
    public void MillCards_StopsEarly_WhenLibraryHasFewer()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);

        game.MillCards(5);

        Assert.Empty(game.Library);
        Assert.Single(game.Graveyard);
    }

    [Fact]
    public void ExileTopCards_MovesCardsFromLibraryToExile()
    {
        var game = CreateGameState();
        var card1 = AddCardToLibrary(game);
        var card2 = AddCardToLibrary(game);

        game.ExileTopCards(2);

        Assert.Empty(game.Library);
        Assert.Equal(2, game.Exile.Count);
        Assert.Equal(GameZone.Exile, card1.Zone);
        Assert.Equal(GameZone.Exile, card2.Zone);
    }

    [Fact]
    public void ExileTopCards_StopsEarly_WhenLibraryHasFewer()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);

        game.ExileTopCards(10);

        Assert.Empty(game.Library);
        Assert.Single(game.Exile);
    }

    // ─── Playing cards from hand ──────────────────────────────────────────────

    [Fact]
    public void PlayCardFromHand_Land_GoesToBattlefieldLands()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game, isLand: true);

        game.PlayCardFromHand(card);

        Assert.Empty(game.Hand);
        Assert.Single(game.BattlefieldLands);
        Assert.Empty(game.Stack);
        Assert.Equal(GameZone.BattlefieldLands, card.Zone);
    }

    [Fact]
    public void PlayCardFromHand_CardTypeContainsLand_GoesToBattlefieldLands()
    {
        var game = CreateGameState();
        // IsLand flag is false but CardType contains "Land"
        var card = MakeCardInHand(game, isLand: false, cardType: "Artifact Land");

        game.PlayCardFromHand(card);

        Assert.Single(game.BattlefieldLands);
        Assert.Empty(game.Stack);
        Assert.Equal(GameZone.BattlefieldLands, card.Zone);
    }

    [Fact]
    public void PlayCardFromHand_NonLand_GoesToStack()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game, isLand: false);

        game.PlayCardFromHand(card);

        Assert.Empty(game.Hand);
        Assert.Single(game.Stack);
        Assert.Empty(game.BattlefieldLands);
        Assert.Equal(GameZone.Stack, card.Zone);
    }

    [Fact]
    public void PlayCardFromHand_DoesNothing_WhenCardIsNotInHand()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard { CardName = "Test", Zone = GameZone.Graveyard, IsFrontFace = true });

        game.PlayCardFromHand(card);

        Assert.Empty(game.Stack);
        Assert.Empty(game.BattlefieldLands);
    }

    // ─── Stack operations ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveStack_MovesTopCardToBattlefieldNonlands()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.PlayCardFromHand(card); // puts card on stack

        game.ResolveStack();

        Assert.Empty(game.Stack);
        Assert.Single(game.BattlefieldNonlands);
        Assert.Equal(GameZone.Battlefield, card.Zone);
    }

    [Fact]
    public void ResolveStack_DoesNothing_WhenStackIsEmpty()
    {
        var game = CreateGameState();

        game.ResolveStack();

        Assert.Empty(game.BattlefieldNonlands);
    }

    [Fact]
    public void CounterStack_MovesTopCardToGraveyard()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.PlayCardFromHand(card);

        game.CounterStack();

        Assert.Empty(game.Stack);
        Assert.Single(game.Graveyard);
        Assert.Equal(GameZone.Graveyard, card.Zone);
    }

    [Fact]
    public void CounterStack_DoesNothing_WhenStackIsEmpty()
    {
        var game = CreateGameState();

        game.CounterStack();

        Assert.Empty(game.Graveyard);
    }

    [Fact]
    public void ReturnStackToHand_MovesTopCardToHand()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.PlayCardFromHand(card);

        game.ReturnStackToHand();

        Assert.Empty(game.Stack);
        Assert.Single(game.Hand);
        Assert.Equal(GameZone.Hand, card.Zone);
    }

    [Fact]
    public void ReturnStackToHand_DoesNothing_WhenStackIsEmpty()
    {
        var game = CreateGameState();

        game.ReturnStackToHand();

        Assert.Empty(game.Hand);
    }

    [Fact]
    public void ExileFromStack_MovesTopCardToExile()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.PlayCardFromHand(card);

        game.ExileFromStack();

        Assert.Empty(game.Stack);
        Assert.Single(game.Exile);
        Assert.Equal(GameZone.Exile, card.Zone);
    }

    [Fact]
    public void ExileFromStack_DoesNothing_WhenStackIsEmpty()
    {
        var game = CreateGameState();

        game.ExileFromStack();

        Assert.Empty(game.Exile);
    }

    // ─── Hand management operations ───────────────────────────────────────────

    [Fact]
    public void SendToTopOfLibrary_MovesCardFromHandToTopOfLibrary()
    {
        var game = CreateGameState();
        var existingCard = AddCardToLibrary(game);
        var handCard = MakeCardInHand(game);

        game.SendToTopOfLibrary(handCard);

        Assert.Empty(game.Hand);
        Assert.Equal(2, game.Library.Count);
        Assert.Same(handCard, game.Library[0]);
        Assert.Equal(GameZone.Library, handCard.Zone);
    }

    [Fact]
    public void SendToTopOfLibrary_DoesNothing_WhenCardIsNotInHand()
    {
        var game = CreateGameState();
        var card = AddCardToLibrary(game);

        game.SendToTopOfLibrary(card);

        // Card was in library, not hand – no change expected
        Assert.Single(game.Library);
        Assert.Empty(game.Hand);
    }

    [Fact]
    public void SendToBottomOfLibrary_MovesCardFromHandToBottomOfLibrary()
    {
        var game = CreateGameState();
        var existingCard = AddCardToLibrary(game);
        var handCard = MakeCardInHand(game);

        game.SendToBottomOfLibrary(handCard);

        Assert.Empty(game.Hand);
        Assert.Equal(2, game.Library.Count);
        Assert.Same(handCard, game.Library[1]);
        Assert.Equal(GameZone.Library, handCard.Zone);
    }

    [Fact]
    public void DiscardFromHand_MovesCardFromHandToTopOfGraveyard()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);

        game.DiscardFromHand(card);

        Assert.Empty(game.Hand);
        Assert.Single(game.Graveyard);
        Assert.Same(card, game.Graveyard[0]);
        Assert.Equal(GameZone.Graveyard, card.Zone);
    }

    [Fact]
    public void DiscardFromHand_DoesNothing_WhenCardIsNotInHand()
    {
        var game = CreateGameState();
        var card = AddCardToLibrary(game);

        game.DiscardFromHand(card);

        Assert.Empty(game.Graveyard);
        Assert.Single(game.Library);
    }

    [Fact]
    public void ExileFromHand_MovesCardFromHandToExile()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);

        game.ExileFromHand(card);

        Assert.Empty(game.Hand);
        Assert.Single(game.Exile);
        Assert.Equal(GameZone.Exile, card.Zone);
    }

    [Fact]
    public void ExileFromHand_DoesNothing_WhenCardIsNotInHand()
    {
        var game = CreateGameState();
        var card = AddCardToLibrary(game);

        game.ExileFromHand(card);

        Assert.Empty(game.Exile);
    }

    // ─── MoveCard generic zone transition ─────────────────────────────────────

    [Fact]
    public void MoveCard_MovesCardFromHandToGraveyard()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);

        game.MoveCard(card, GameZone.Graveyard);

        Assert.Empty(game.Hand);
        Assert.Single(game.Graveyard);
        Assert.Equal(GameZone.Graveyard, card.Zone);
    }

    [Fact]
    public void MoveCard_MovesCardFromLibraryToHand()
    {
        var game = CreateGameState();
        var card = AddCardToLibrary(game);

        game.MoveCard(card, GameZone.Hand);

        Assert.Empty(game.Library);
        Assert.Single(game.Hand);
        Assert.Equal(GameZone.Hand, card.Zone);
    }

    [Fact]
    public void MoveCard_Token_CeasesToExist_WhenLeavingBattlefield()
    {
        var game = CreateGameState();
        var token = CreateCardVm();
        token.InitializeFrom(new PlaytestCard
        {
            CardName = "Soldier Token",
            IsToken = true,
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        game.BattlefieldNonlands.Add(token);

        game.MoveCard(token, GameZone.Graveyard);

        Assert.Empty(game.BattlefieldNonlands);
        Assert.Empty(game.Graveyard);
    }

    [Fact]
    public void MoveCard_Token_DoesNotCeaseToExist_WhenMovingBetweenBattlefieldZones()
    {
        var game = CreateGameState();
        var token = CreateCardVm();
        token.InitializeFrom(new PlaytestCard
        {
            CardName = "Land Token",
            IsToken = true,
            IsLand = true,
            Zone = GameZone.BattlefieldLands,
            IsFrontFace = true,
        });
        game.BattlefieldLands.Add(token);

        // Move from BattlefieldLands to Battlefield (another battlefield zone)
        game.MoveCard(token, GameZone.Battlefield);

        Assert.Empty(game.BattlefieldLands);
        Assert.Single(game.BattlefieldNonlands);
    }

    [Fact]
    public void MoveCard_Token_SelectedCardIsCleared_WhenTokenCeasesToExist()
    {
        var game = CreateGameState();
        var token = CreateCardVm();
        token.InitializeFrom(new PlaytestCard
        {
            CardName = "Goblin Token",
            IsToken = true,
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        game.BattlefieldNonlands.Add(token);
        game.SelectedCard = token;

        game.MoveCard(token, GameZone.Graveyard);

        Assert.Null(game.SelectedCard);
    }

    // ─── Phase management ─────────────────────────────────────────────────────

    [Fact]
    public void AdvancePhaseCommand_MovesToNextPhase()
    {
        var game = CreateGameState();
        Assert.Equal(GamePhase.Untap, game.CurrentPhase);

        game.AdvancePhaseCommand.Execute(null);

        Assert.Equal(GamePhase.Upkeep, game.CurrentPhase);
        Assert.Equal(1, game.CurrentPhaseIndex);
    }

    [Fact]
    public void AdvancePhaseCommand_FromUntap_UntapsAllBattlefieldCards()
    {
        var game = CreateGameState();
        var land = CreateCardVm();
        land.InitializeFrom(new PlaytestCard { CardName = "Forest", IsLand = true, Zone = GameZone.BattlefieldLands, IsFrontFace = true, IsTapped = true });
        game.BattlefieldLands.Add(land);

        var creature = CreateCardVm();
        creature.InitializeFrom(new PlaytestCard { CardName = "Grizzly Bears", Zone = GameZone.Battlefield, IsFrontFace = true, IsTapped = true });
        game.BattlefieldNonlands.Add(creature);

        game.AdvancePhaseCommand.Execute(null);

        Assert.False(land.IsTapped);
        Assert.False(creature.IsTapped);
    }

    [Fact]
    public void AdvancePhaseCommand_FromCleanup_WrapsBackToUntap()
    {
        var game = CreateGameState();
        // Advance to Cleanup (index 11)
        for (int i = 0; i < 11; i++)
            game.AdvancePhaseCommand.Execute(null);

        Assert.Equal(GamePhase.Cleanup, game.CurrentPhase);

        game.AdvancePhaseCommand.Execute(null);

        Assert.Equal(GamePhase.Untap, game.CurrentPhase);
        Assert.Equal(0, game.CurrentPhaseIndex);
    }

    [Fact]
    public void EndTurnCommand_ResetsPhaseToUntap()
    {
        var game = CreateGameState();
        game.AdvancePhaseCommand.Execute(null); // Upkeep
        game.AdvancePhaseCommand.Execute(null); // Draw

        game.EndTurnCommand.Execute(null);

        Assert.Equal(GamePhase.Untap, game.CurrentPhase);
        Assert.Equal(0, game.CurrentPhaseIndex);
    }

    [Theory]
    [InlineData(GamePhase.Untap, "Untap")]
    [InlineData(GamePhase.Upkeep, "Upkeep")]
    [InlineData(GamePhase.Draw, "Draw")]
    [InlineData(GamePhase.MainPhase1, "Main")]
    [InlineData(GamePhase.BeginCombat, "Begin Combat")]
    [InlineData(GamePhase.DeclareAttackers, "Attack")]
    [InlineData(GamePhase.DeclareBlockers, "Block")]
    [InlineData(GamePhase.CombatDamage, "Damage")]
    [InlineData(GamePhase.EndCombat, "End Combat")]
    [InlineData(GamePhase.MainPhase2, "Main")]
    [InlineData(GamePhase.End, "End")]
    [InlineData(GamePhase.Cleanup, "Pass")]
    public void PhaseDisplayName_ReturnsCorrectLabel(GamePhase phase, string expectedLabel)
    {
        var game = CreateGameState();
        game.CurrentPhase = phase;

        Assert.Equal(expectedLabel, game.PhaseDisplayName);
    }

    // ─── Computed properties ──────────────────────────────────────────────────

    [Fact]
    public void LibraryCount_TracksLibrarySize()
    {
        var game = CreateGameState();
        Assert.Equal(0, game.LibraryCount);

        AddCardToLibrary(game);
        AddCardToLibrary(game);

        Assert.Equal(2, game.LibraryCount);
    }

    [Fact]
    public void LibraryCount_DecreasesAfterDrawCard()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        AddCardToLibrary(game);
        AddCardToLibrary(game);

        game.DrawCardCommand.Execute(null);

        Assert.Equal(2, game.LibraryCount);
    }

    [Fact]
    public void TopGraveyardCard_ReturnsFirstElementInGraveyard()
    {
        var game = CreateGameState();
        var card1 = MakeCardInHand(game);
        var card2 = MakeCardInHand(game);
        game.DiscardFromHand(card1);
        game.DiscardFromHand(card2);

        // DiscardFromHand inserts at index 0, so card2 is now at top
        Assert.Same(card2, game.TopGraveyardCard);
    }

    [Fact]
    public void TopGraveyardCard_IsNull_WhenGraveyardIsEmpty()
    {
        var game = CreateGameState();
        Assert.Null(game.TopGraveyardCard);
    }

    [Fact]
    public void TopExileCard_ReturnsFirstElementInExile()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.ExileFromHand(card);

        Assert.Same(card, game.TopExileCard);
    }

    [Fact]
    public void TopExileCard_IsNull_WhenExileIsEmpty()
    {
        var game = CreateGameState();
        Assert.Null(game.TopExileCard);
    }

    [Fact]
    public void CardWidth_IsScaledByCardScale()
    {
        var game = CreateGameState();
        game.CardScale = 1.5;

        Assert.Equal(150, game.CardWidth);   // 100 * 1.5
        Assert.Equal(210, game.CardHeight);  // 140 * 1.5
    }

    // ─── ResetGame ────────────────────────────────────────────────────────────

    [Fact]
    public void ResetGameCommand_ClearsAllZonesAndReturnsNonTokensToLibrary()
    {
        var game = CreateGameState();

        // Populate a few zones
        var libCard = AddCardToLibrary(game);
        var handCard = MakeCardInHand(game);
        var gravCard = MakeCardInHand(game);
        game.DiscardFromHand(gravCard);

        game.ResetGameCommand.Execute(null);

        // All zones except Library should be empty
        Assert.Empty(game.Hand);
        Assert.Empty(game.Graveyard);
        Assert.Empty(game.Exile);
        Assert.Empty(game.Stack);
        Assert.Empty(game.BattlefieldLands);
        Assert.Empty(game.BattlefieldNonlands);

        // All three non-token cards should be in the library
        Assert.Equal(3, game.Library.Count);
    }

    [Fact]
    public void ResetGameCommand_RemovesTokensFromGame()
    {
        var game = CreateGameState();
        var token = CreateCardVm();
        token.InitializeFrom(new PlaytestCard
        {
            CardName = "Elf Token",
            IsToken = true,
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        game.BattlefieldNonlands.Add(token);

        game.ResetGameCommand.Execute(null);

        Assert.DoesNotContain(token, game.Library);
        Assert.DoesNotContain(token, game.BattlefieldNonlands);
    }

    [Fact]
    public void ResetGameCommand_ResetsCountersToDefaults()
    {
        var game = CreateGameState();
        game.LifeTotal = 5;
        game.WhiteMana = 3;
        game.BlueMana = 2;
        game.BlackMana = 1;
        game.RedMana = 4;
        game.GreenMana = 2;
        game.ColorlessMana = 7;
        game.StormCount = 5;
        game.EnergyCount = 3;

        game.ResetGameCommand.Execute(null);

        Assert.Equal(20, game.LifeTotal);
        Assert.Equal(0, game.WhiteMana);
        Assert.Equal(0, game.BlueMana);
        Assert.Equal(0, game.BlackMana);
        Assert.Equal(0, game.RedMana);
        Assert.Equal(0, game.GreenMana);
        Assert.Equal(0, game.ColorlessMana);
        Assert.Equal(0, game.StormCount);
        Assert.Equal(0, game.EnergyCount);
    }

    [Fact]
    public void ResetGameCommand_ResetsPhaseToUntap()
    {
        var game = CreateGameState();
        game.AdvancePhaseCommand.Execute(null); // Upkeep
        game.AdvancePhaseCommand.Execute(null); // Draw

        game.ResetGameCommand.Execute(null);

        Assert.Equal(GamePhase.Untap, game.CurrentPhase);
        Assert.Equal(0, game.CurrentPhaseIndex);
    }

    [Fact]
    public void ResetGameCommand_UntapsCardsReturnedToLibrary()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.PlayCardFromHand(card); // → stack
        game.ResolveStack();         // → battlefield
        card.IsTapped = true;

        game.ResetGameCommand.Execute(null);

        // After reset the card is back in library and should not be tapped
        var returnedCard = game.Library.Single(c => c.CardName == card.CardName);
        Assert.False(returnedCard.IsTapped);
        Assert.Equal(GameZone.Library, returnedCard.Zone);
    }

    [Fact]
    public void ResetGameCommand_ClearsSelectedCard()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);
        game.SelectedCard = card;

        game.ResetGameCommand.Execute(null);

        Assert.Null(game.SelectedCard);
    }

    // ─── InitializeWithDeck ───────────────────────────────────────────────────

    [Fact]
    public async Task InitializeWithDeck_LoadsMainDeckCardsIntoLibrary()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Id = 1,
            Name = "Test Deck",
            MainDeck =
            [
                new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" },
                new DeckCardModel { CardName = "Lightning Bolt", IsLand = false, CardType = "Instant", Edition = "M21" },
                new DeckCardModel { CardName = "Grizzly Bears", IsLand = false, CardType = "Creature", Edition = "M21", Power = "2", Toughness = "2" },
            ],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        Assert.Equal(3, game.Library.Count);
        Assert.Equal(3, game.LibraryCount);
        Assert.Empty(game.Hand);
    }

    [Fact]
    public async Task InitializeWithDeck_SetsCorrectCardProperties()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Id = 1,
            Name = "Test Deck",
            MainDeck =
            [
                new DeckCardModel
                {
                    CardName = "Grizzly Bears",
                    IsLand = false,
                    CardType = "Creature — Bear",
                    CastingCost = "{1}{G}",
                    OracleText = "Rawr.",
                    Power = "2",
                    Toughness = "2",
                    Edition = "M21",
                }
            ],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        var card = game.Library.Single();
        Assert.Equal("Grizzly Bears", card.CardName);
        Assert.Equal("{1}{G}", card.ManaCost);
        Assert.Equal("Creature — Bear", card.CardType);
        Assert.Equal("2", card.Power);
        Assert.Equal("2", card.Toughness);
        Assert.Equal("Rawr.", card.OracleText);
        Assert.False(card.IsLand);
        Assert.Equal(GameZone.Library, card.Zone);
        Assert.False(card.IsTapped);
        Assert.True(card.IsFrontFace);
    }

    [Fact]
    public async Task InitializeWithDeck_ClearsHandAndResetsCounters()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        // Pre-populate the hand and set non-default counters
        MakeCardInHand(game);
        game.LifeTotal = 7;
        game.StormCount = 3;

        var deck = new DeckModel
        {
            Id = 2,
            Name = "New Deck",
            MainDeck = [new DeckCardModel { CardName = "Island", IsLand = true, Edition = "M21" }],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        // Hand and counters are reset by the internal ResetGame call
        Assert.Empty(game.Hand);
        Assert.Equal(20, game.LifeTotal);
        Assert.Equal(0, game.StormCount);
    }
}

/// <summary>
/// Tests for <see cref="PlaytestingViewModel"/> covering deck-selection state,
/// deck list refresh and the SelectDeck command.
/// </summary>
public class PlaytestingViewModelTests
{
    private static PlaytestingViewModel CreateViewModel(
        ICollectionTrackingService? service = null,
        IScryfallApiClient? scryfallClient = null,
        Func<PlaytestGameStateViewModel>? gameStateFactory = null)
    {
        var messenger = new WeakReferenceMessenger();
        service ??= new Mock<ICollectionTrackingService>().Object;
        scryfallClient ??= new Mock<IScryfallApiClient>().Object;
        gameStateFactory ??= () =>
        {
            var mockFs = new Mock<ICardImageFileSystem>();
            mockFs.Setup(f => f.TryGetStream(It.IsAny<string>(), It.IsAny<string>())).Returns((Stream?)null);
            var mockClient = new Mock<IScryfallApiClient>();
            Func<StrongInject.Owned<MtgCollectionTracker.Data.CardsDbContext>> neverInvoked =
                () => throw new InvalidOperationException("DB not needed");
            var cache = new CardImageCache(neverInvoked, mockFs.Object, mockClient.Object);
            return new PlaytestGameStateViewModel(messenger, () => new PlaytestCardViewModel(cache));
        };
        return new PlaytestingViewModel(service, scryfallClient, gameStateFactory, messenger);
    }

    [Fact]
    public void IsDeckSelected_IsFalse_WhenNoDeckSelected()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsDeckSelected);
    }

    [Fact]
    public void IsDeckSelected_IsTrue_AfterDeckIsSelected()
    {
        var vm = CreateViewModel();
        vm.SelectedDeck = new DeckSummaryModel { Id = 1, Name = "Burn", DeckName = "Burn" };

        Assert.True(vm.IsDeckSelected);
    }

    [Fact]
    public void IsDeckSelected_IsFalse_AfterDeckSelectionIsCleared()
    {
        var vm = CreateViewModel();
        vm.SelectedDeck = new DeckSummaryModel { Id = 1, Name = "Burn", DeckName = "Burn" };

        vm.SelectedDeck = null;

        Assert.False(vm.IsDeckSelected);
    }

    [Fact]
    public void BeginPlaytestCommand_CannotExecute_WhenNoDeckSelected()
    {
        var vm = CreateViewModel();

        Assert.False(vm.BeginPlaytestCommand.CanExecute(null));
    }

    [Fact]
    public void BeginPlaytestCommand_CanExecute_WhenDeckIsSelected()
    {
        var vm = CreateViewModel();
        vm.SelectedDeck = new DeckSummaryModel { Id = 1, Name = "Burn", DeckName = "Burn" };

        Assert.True(vm.BeginPlaytestCommand.CanExecute(null));
    }

    [Fact]
    public void RefreshDecksCommand_LoadsDecksAlphabetically()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        // The constructor triggers OnActivated which calls RefreshDecks when the list is empty.
        // Use SetupSequence so the first (automatic) call returns empty and the explicit test call
        // returns the full list.
        mockService.SetupSequence(s => s.GetDecks(null))
            .Returns([])
            .Returns([
                new DeckSummaryModel { Id = 3, Name = "Zoo", DeckName = "Zoo" },
                new DeckSummaryModel { Id = 1, Name = "Burn", DeckName = "Burn" },
                new DeckSummaryModel { Id = 2, Name = "Control", DeckName = "Control" },
            ]);

        var vm = CreateViewModel(service: mockService.Object);
        vm.RefreshDecksCommand.Execute(null);

        Assert.Equal(3, vm.AvailableDecks.Count);
        Assert.Equal("Burn", vm.AvailableDecks[0].Name);
        Assert.Equal("Control", vm.AvailableDecks[1].Name);
        Assert.Equal("Zoo", vm.AvailableDecks[2].Name);
    }

    [Fact]
    public void RefreshDecksCommand_ClearsExistingDecksBeforeReloading()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        // First call is from the automatic OnActivated → returns empty.
        // Subsequent calls are explicit test invocations.
        mockService.SetupSequence(s => s.GetDecks(null))
            .Returns([])
            .Returns([new DeckSummaryModel { Id = 1, Name = "First Deck", DeckName = "First Deck" }])
            .Returns([new DeckSummaryModel { Id = 2, Name = "Second Deck", DeckName = "Second Deck" }]);

        var vm = CreateViewModel(service: mockService.Object);

        vm.RefreshDecksCommand.Execute(null);
        Assert.Single(vm.AvailableDecks);
        Assert.Equal("First Deck", vm.AvailableDecks[0].Name);

        vm.RefreshDecksCommand.Execute(null);
        Assert.Single(vm.AvailableDecks);
        Assert.Equal("Second Deck", vm.AvailableDecks[0].Name);
    }

    [Fact]
    public void SelectDeckCommand_ResetsIsInGameToFalse()
    {
        var vm = CreateViewModel();
        vm.IsInGame = true;

        vm.SelectDeckCommand.Execute(null);

        Assert.False(vm.IsInGame);
    }

    [Fact]
    public void SelectDeckCommand_ClearsSelectedDeck()
    {
        var vm = CreateViewModel();
        vm.SelectedDeck = new DeckSummaryModel { Id = 1, Name = "Burn", DeckName = "Burn" };

        vm.SelectDeckCommand.Execute(null);

        Assert.Null(vm.SelectedDeck);
    }

    [Fact]
    public void SelectDeckCommand_ClearsGameState()
    {
        var vm = CreateViewModel();

        // Manually assign a game state to simulate being in a game
        vm.SelectDeckCommand.Execute(null);

        Assert.Null(vm.GameState);
        Assert.False(vm.IsInGame);
    }
}
