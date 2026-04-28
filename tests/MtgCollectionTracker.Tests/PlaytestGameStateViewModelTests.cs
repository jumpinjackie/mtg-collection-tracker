using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Tests;

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
    public void CardWidth_IsScaledByBattlefieldCardScale()
    {
        var game = CreateGameState();
        game.BattlefieldCardScale = 1.5;

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

    // ─── Commander Support ────────────────────────────────────────────────────

    [Fact]
    public void CommanderTaxCounter_IncrementAndDecrement()
    {
        var game = CreateGameState();

        game.IncrementCommanderTaxCommand.Execute(null);
        game.IncrementCommanderTaxCommand.Execute(null);
        Assert.Equal(2, game.CommanderTax);

        game.DecrementCommanderTaxCommand.Execute(null);
        Assert.Equal(1, game.CommanderTax);
    }

    [Fact]
    public void CommanderTaxCounter_DoesNotGoBelowZero()
    {
        var game = CreateGameState();

        game.DecrementCommanderTaxCommand.Execute(null);

        Assert.Equal(0, game.CommanderTax);
    }

    [Fact]
    public void CommanderDamageCounter_IncrementAndDecrement()
    {
        var game = CreateGameState();

        game.IncrementCommanderDamageCommand.Execute(null);
        game.IncrementCommanderDamageCommand.Execute(null);
        Assert.Equal(2, game.CommanderDamage);

        game.DecrementCommanderDamageCommand.Execute(null);
        Assert.Equal(1, game.CommanderDamage);
    }

    [Fact]
    public void CommanderDamageCounter_DoesNotGoBelowZero()
    {
        var game = CreateGameState();

        game.DecrementCommanderDamageCommand.Execute(null);

        Assert.Equal(0, game.CommanderDamage);
    }

    [Fact]
    public void ResetGameCommand_ResetsCommanderCounters()
    {
        var game = CreateGameState();
        game.CommanderTax = 4;
        game.CommanderDamage = 7;

        game.ResetGameCommand.Execute(null);

        Assert.Equal(0, game.CommanderTax);
        Assert.Equal(0, game.CommanderDamage);
    }

    [Fact]
    public async Task InitializeWithDeck_CommanderDeck_PlacesCommanderInCommandZone()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var commanderCard = new DeckCardModel
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Edition = "C16",
            Power = "4",
            Toughness = "4",
        };

        var deck = new DeckModel
        {
            Id = 1,
            Name = "Atraxa Commander",
            IsCommander = true,
            Commander = commanderCard,
            MainDeck =
            [
                new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" },
                new DeckCardModel { CardName = "Lightning Bolt", IsLand = false, CardType = "Instant", Edition = "M21" },
            ],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        Assert.True(game.IsCommanderGame);
        Assert.True(game.HasCommandZone);
        Assert.Single(game.CommandZone);
        Assert.Equal("Atraxa, Praetors' Voice", game.CommandZone[0].CardName);
        Assert.Equal(GameZone.CommandZone, game.CommandZone[0].Zone);

        // Commander should NOT be in the library
        Assert.Equal(2, game.Library.Count);
    }

    [Fact]
    public async Task InitializeWithDeck_NonCommanderDeck_HasEmptyCommandZone()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Id = 1,
            Name = "Regular Deck",
            IsCommander = false,
            Commander = null,
            MainDeck =
            [
                new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" },
            ],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        Assert.False(game.IsCommanderGame);
        Assert.False(game.HasCommandZone);
        Assert.Empty(game.CommandZone);
    }

    [Fact]
    public async Task ResetGameCommand_CommanderReturnsToCommandZone()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var commanderCard = new DeckCardModel
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Edition = "C16",
        };

        var deck = new DeckModel
        {
            Id = 1,
            Name = "Atraxa Commander",
            IsCommander = true,
            Commander = commanderCard,
            MainDeck =
            [
                new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" },
            ],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        // Move commander from command zone to battlefield
        var commander = game.CommandZone.First();
        game.MoveCard(commander, GameZone.Battlefield);
        Assert.Empty(game.CommandZone);
        Assert.Single(game.BattlefieldNonlands);

        // Reset should return commander to command zone
        game.ResetGameCommand.Execute(null);

        Assert.Single(game.CommandZone);
        Assert.Equal("Atraxa, Praetors' Voice", game.CommandZone[0].CardName);
        Assert.Equal(GameZone.CommandZone, game.CommandZone[0].Zone);
        Assert.Empty(game.BattlefieldNonlands);
    }

    [Fact]
    public void MoveCard_FromCommandZone_ToHand_Works()
    {
        var game = CreateGameState();

        var commander = CreateCardVm();
        commander.InitializeFrom(new PlaytestCard
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Zone = GameZone.CommandZone,
            IsFrontFace = true,
        });
        game.CommandZone.Add(commander);

        game.MoveCard(commander, GameZone.Hand);

        Assert.Empty(game.CommandZone);
        Assert.Single(game.Hand);
        Assert.Equal(GameZone.Hand, commander.Zone);
    }

    [Fact]
    public void CommandZoneViewModel_MoveToTopOfLibrary_InvokesTopLibraryAction()
    {
        var messenger = new WeakReferenceMessenger();
        var commandZone = new System.Collections.ObjectModel.ObservableCollection<PlaytestCardViewModel>();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Zone = GameZone.CommandZone,
            IsFrontFace = true,
        });
        commandZone.Add(card);

        PlaytestCardViewModel? receivedCard = null;
        var vm = new CommandZoneViewModel(messenger).Configure(
            commandZone,
            moveCard: (c, z) => { },
            moveToTopOfLibrary: c => receivedCard = c,
            moveToBottomOfLibrary: c => { },
            moveToLibraryAndShuffle: c => { });

        vm.MoveToTopOfLibraryCommand.Execute(null);

        Assert.NotNull(receivedCard);
        Assert.Equal("Atraxa, Praetors' Voice", receivedCard!.CardName);
    }

    [Fact]
    public void CommandZoneViewModel_MoveToBottomOfLibrary_InvokesBottomLibraryAction()
    {
        var messenger = new WeakReferenceMessenger();
        var commandZone = new System.Collections.ObjectModel.ObservableCollection<PlaytestCardViewModel>();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Zone = GameZone.CommandZone,
            IsFrontFace = true,
        });
        commandZone.Add(card);

        PlaytestCardViewModel? receivedCard = null;
        var vm = new CommandZoneViewModel(messenger).Configure(
            commandZone,
            moveCard: (c, z) => { },
            moveToTopOfLibrary: c => { },
            moveToBottomOfLibrary: c => receivedCard = c,
            moveToLibraryAndShuffle: c => { });

        vm.MoveToBottomOfLibraryCommand.Execute(null);

        Assert.NotNull(receivedCard);
        Assert.Equal("Atraxa, Praetors' Voice", receivedCard!.CardName);
    }

    [Fact]
    public void CommandZoneViewModel_MoveToLibraryAndShuffle_InvokesShuffleAction()
    {
        var messenger = new WeakReferenceMessenger();
        var commandZone = new System.Collections.ObjectModel.ObservableCollection<PlaytestCardViewModel>();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Zone = GameZone.CommandZone,
            IsFrontFace = true,
        });
        commandZone.Add(card);

        PlaytestCardViewModel? receivedCard = null;
        var vm = new CommandZoneViewModel(messenger).Configure(
            commandZone,
            moveCard: (c, z) => { },
            moveToTopOfLibrary: c => { },
            moveToBottomOfLibrary: c => { },
            moveToLibraryAndShuffle: c => receivedCard = c);

        vm.MoveToLibraryAndShuffleCommand.Execute(null);

        Assert.NotNull(receivedCard);
        Assert.Equal("Atraxa, Praetors' Voice", receivedCard!.CardName);
    }

    [Fact]
    public async Task OpenCommandZoneDialog_MoveToBottomOfLibrary_AppendedAtEnd()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        var commanderCard = new DeckCardModel
        {
            CardName = "Atraxa, Praetors' Voice",
            IsLand = false,
            CardType = "Legendary Creature",
            Edition = "C16",
        };
        var deck = new DeckModel
        {
            Name = "Atraxa Commander",
            IsCommander = true,
            Commander = commanderCard,
            MainDeck =
            [
                new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" },
            ],
            Sideboard = [],
        };

        var game = CreateGameState();
        await game.InitializeWithDeck(deck, mockService.Object);

        var commander = game.CommandZone.First();
        int libraryCountBefore = game.Library.Count;

        // MoveCard to Library uses AddToZone → Library.Add (bottom of library)
        game.MoveCard(commander, GameZone.Library);

        Assert.Empty(game.CommandZone);
        Assert.Equal(libraryCountBefore + 1, game.Library.Count);
        Assert.Equal(GameZone.Library, commander.Zone);
        // Commander should be at the end (bottom of library since index 0 is top)
        Assert.Same(commander, game.Library[^1]);
    }

    // ─── Sideboard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeWithDeck_LoadsSideboardCards()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Name = "Test Deck",
            MainDeck = [new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" }],
            Sideboard =
            [
                new DeckCardModel { CardName = "Relic of Progenitus", IsLand = false, CardType = "Artifact", Edition = "ALA" },
            ],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        Assert.Single(game.Sideboard);
        Assert.Equal("Relic of Progenitus", game.Sideboard[0].CardName);
        Assert.Equal(GameZone.Sideboard, game.Sideboard[0].Zone);
        Assert.True(game.HasSideboard);
    }

    [Fact]
    public async Task InitializeWithDeck_EmptySideboard_HasSideboardIsFalse()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Name = "Test Deck",
            MainDeck = [new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" }],
            Sideboard = [],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        Assert.Empty(game.Sideboard);
        Assert.False(game.HasSideboard);
    }

    [Fact]
    public async Task ResetGame_SideboardCardMovedToHand_RestoredToSideboardNoDuplicates()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Name = "Test Deck",
            MainDeck = [new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" }],
            Sideboard =
            [
                new DeckCardModel { CardName = "Tormod's Crypt", IsLand = false, CardType = "Artifact", Edition = "M21" },
            ],
        };

        await game.InitializeWithDeck(deck, mockService.Object);

        // Move the sideboard card to hand (simulates player putting it into play)
        var sbCard = game.Sideboard[0];
        game.MoveCard(sbCard, GameZone.Hand);
        Assert.Empty(game.Sideboard);
        Assert.Single(game.Hand);

        // Reset — the card should be back in sideboard, NOT also in library
        game.ResetGameCommand.Execute(null);

        Assert.Single(game.Sideboard);
        Assert.Equal("Tormod's Crypt", game.Sideboard[0].CardName);
        Assert.Equal(GameZone.Sideboard, game.Sideboard[0].Zone);

        // The original "Forest" main deck card should still be in the library
        Assert.Single(game.Library);
        Assert.Equal("Forest", game.Library[0].CardName);
    }

    [Fact]
    public async Task ResetGame_MultipleCopiesOfSideboardCard_AllRestoredNoDuplicates()
    {
        var game = CreateGameState();
        var mockService = new Mock<ICollectionTrackingService>();

        var deck = new DeckModel
        {
            Name = "Test Deck",
            MainDeck = [new DeckCardModel { CardName = "Forest", IsLand = true, CardType = "Basic Land", Edition = "M21" }],
            Sideboard =
            [
                new DeckCardModel { CardName = "Relic of Progenitus", IsLand = false, CardType = "Artifact", Edition = "ALA" },
                new DeckCardModel { CardName = "Relic of Progenitus", IsLand = false, CardType = "Artifact", Edition = "ALA" },
            ],
        };

        await game.InitializeWithDeck(deck, mockService.Object);
        Assert.Equal(2, game.Sideboard.Count);

        // Move both copies to hand
        game.MoveCard(game.Sideboard[0], GameZone.Hand);
        game.MoveCard(game.Sideboard[0], GameZone.Hand);
        Assert.Empty(game.Sideboard);

        game.ResetGameCommand.Execute(null);

        Assert.Equal(2, game.Sideboard.Count);
        Assert.All(game.Sideboard, c => Assert.Equal("Relic of Progenitus", c.CardName));
        // Only the one main-deck card in library — no duplicates from sideboard restoration
        Assert.Single(game.Library);
        Assert.Equal("Forest", game.Library[0].CardName);
    }

    // ─── SelectedBattlefieldCards sync ────────────────────────────────────────

    [Fact]
    public void MoveCard_SelectedBattlefieldCard_RemovedFromSelectionOnMove()
    {
        var game = CreateGameState();

        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Lightning Bolt",
            CardType = "Instant",
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        game.BattlefieldNonlands.Add(card);

        // Select the card
        game.ToggleBattlefieldCardSelection(card, addToSelection: true);
        Assert.True(card.IsSelected);
        Assert.Single(game.SelectedBattlefieldCards);

        // Move card off the battlefield via single-card action
        game.MoveCard(card, GameZone.Graveyard);

        Assert.False(card.IsSelected);
        Assert.Empty(game.SelectedBattlefieldCards);
    }

    [Fact]
    public void TapSelectedCards_StaleSelectionPruned_DoesNotAffectMovedCard()
    {
        var game = CreateGameState();

        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Grizzly Bears",
            CardType = "Creature",
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        game.BattlefieldNonlands.Add(card);

        game.ToggleBattlefieldCardSelection(card, addToSelection: true);

        // Forcefully move without going through MoveCard to simulate a stale reference
        game.BattlefieldNonlands.Remove(card);
        card.Zone = GameZone.Graveyard;
        game.Graveyard.Add(card);
        // card.IsSelected is still true and still in SelectedBattlefieldCards at this point

        // TapSelectedCards should prune it and NOT tap the already-moved card
        game.TapSelectedCards();

        Assert.Empty(game.SelectedBattlefieldCards);
        Assert.False(card.IsSelected);
    }

    // ─── Game Log ─────────────────────────────────────────────────────────────

    [Fact]
    public void DrawCard_AddsDrawLogEntry()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);

        game.DrawCardCommand.Execute(null);

        Assert.Single(game.GameLog);
        Assert.Equal("Player draws 1 card", game.GameLog[0].Message);
    }

    [Fact]
    public void DrawCards_AddsDrawLogEntryWithCount()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        AddCardToLibrary(game);
        AddCardToLibrary(game);

        game.DrawCards(3);

        Assert.NotEmpty(game.GameLog);
        var drawEntry = game.GameLog.Last();
        Assert.Equal("Player draws 3 cards", drawEntry.Message);
    }

    [Fact]
    public void MillCards_AddsMillLogEntry()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        AddCardToLibrary(game);

        game.MillCards(2);

        Assert.NotEmpty(game.GameLog);
        var millEntry = game.GameLog.Last();
        Assert.Equal("Player mills 2 cards", millEntry.Message);
    }

    [Fact]
    public void PlayCardFromHand_LandCard_AddsBattlefieldLogEntry()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game, isLand: true);

        game.PlayCardFromHand(card);

        Assert.Contains(game.GameLog, e => e.Message == $"Player puts {card.CardName} onto the battlefield");
    }

    [Fact]
    public void PlayCardFromHand_NonLandCard_AddsStackLogEntry()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game, isLand: false);

        game.PlayCardFromHand(card);

        Assert.Contains(game.GameLog, e => e.Message == $"Player puts {card.CardName} onto the stack");
    }

    [Fact]
    public void ResolveStack_AddsResolveLogEntry()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game, isLand: false);
        game.PlayCardFromHand(card);

        game.ResolveStack();

        Assert.Contains(game.GameLog, e => e.Message == $"Player resolves {card.CardName}");
    }

    [Fact]
    public void MoveCard_AddsGenericZoneMoveLogEntry()
    {
        var game = CreateGameState();
        var card = MakeCardInHand(game);

        game.DiscardFromHand(card);

        Assert.Contains(game.GameLog, e => e.Message == $"Player moves {card.CardName} from [Hand] to [Graveyard]");
    }

    [Fact]
    public void ShuffleLibrary_AddsShuffleLogEntry()
    {
        var game = CreateGameState();

        game.ShuffleLibraryCommand.Execute(null);

        Assert.Contains(game.GameLog, e => e.Message == "Player shuffles library");
    }

    [Fact]
    public void AdvancePhase_AddsPhaseLogEntry()
    {
        var game = CreateGameState();

        game.AdvancePhaseCommand.Execute(null); // Move to Upkeep

        Assert.Contains(game.GameLog, e => e.Message.StartsWith("Player advances to phase ("));
    }

    [Fact]
    public void ToggleTap_TapsCard_AddsTapLogEntry()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Sol Ring",
            CardType = "Artifact",
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });

        game.ToggleTap(card);

        Assert.True(card.IsTapped);
        Assert.Contains(game.GameLog, e => e.Message == "Player taps Sol Ring");
    }

    [Fact]
    public void ToggleTap_UntapsCard_AddsUntapLogEntry()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Sol Ring",
            CardType = "Artifact",
            Zone = GameZone.Battlefield,
            IsTapped = true,
            IsFrontFace = true,
        });

        game.ToggleTap(card);

        Assert.False(card.IsTapped);
        Assert.Contains(game.GameLog, e => e.Message == "Player untaps Sol Ring");
    }

    [Fact]
    public void IncrementLife_AddsLifeLogEntry()
    {
        var game = CreateGameState();

        game.IncrementLifeCommand.Execute(null);

        Assert.Contains(game.GameLog, e => e.Message == "Player's life is now 21 (+1)");
    }

    [Fact]
    public void DecrementLife_AddsLifeLogEntry()
    {
        var game = CreateGameState();

        game.DecrementLifeCommand.Execute(null);

        Assert.Contains(game.GameLog, e => e.Message == "Player's life is now 19 (-1)");
    }

    [Fact]
    public void IncrementStorm_AddsStormLogEntry()
    {
        var game = CreateGameState();

        game.IncrementStormCommand.Execute(null);

        Assert.Contains(game.GameLog, e => e.Message == "Player's Storm is now 1 (+1)");
    }

    [Fact]
    public void ResetGame_ClearsGameLog()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        game.DrawCardCommand.Execute(null); // Creates a log entry

        Assert.NotEmpty(game.GameLog);

        game.ResetGameCommand.Execute(null);

        Assert.Empty(game.GameLog);
    }

    [Fact]
    public void AdjustCounter_Increment_AddsCounterLogEntry()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Scute Swarm",
            CardType = "Creature",
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        card.Counters.Add(new CardCounterViewModel { CounterName = "+1/+1", Quantity = 1 });

        game.AdjustCounter(card, "+1/+1", 2);

        Assert.Contains(game.GameLog, e => e.Message == "Player puts 2 +1/+1 counters on Scute Swarm");
    }

    [Fact]
    public void AdjustCounter_Decrement_AddsRemoveCounterLogEntry()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Scute Swarm",
            CardType = "Creature",
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });
        card.Counters.Add(new CardCounterViewModel { CounterName = "+1/+1", Quantity = 3 });

        game.AdjustCounter(card, "+1/+1", -1);

        Assert.Contains(game.GameLog, e => e.Message == "Player removes 1 +1/+1 counter from Scute Swarm");
    }

    [Fact]
    public void PerZoneScales_InitializeToSameDefault()
    {
        var game = CreateGameState();

        Assert.Equal(1.25, game.BattlefieldCardScale);
        Assert.Equal(1.25, game.LandsCardScale);
        Assert.Equal(1.25, game.HandCardScale);
    }

    [Fact]
    public void LandsCardWidth_UsesLandsCardScale()
    {
        var game = CreateGameState();
        game.LandsCardScale = 2.0;

        Assert.Equal(200, game.LandsCardWidth);
        Assert.Equal(280, game.LandsCardHeight);
    }

    [Fact]
    public void HandCardWidth_UsesHandCardScale()
    {
        var game = CreateGameState();
        game.HandCardScale = 0.75;

        Assert.Equal(75, game.HandCardWidth);
        Assert.Equal(105, game.HandCardHeight);
    }

    [Fact]
    public void OpenViewTopXDialog_AddsViewTopXLogEntry()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);
        AddCardToLibrary(game);
        AddCardToLibrary(game);

        game.OpenViewTopXDialog(3);

        Assert.Contains(game.GameLog, e => e.Message == "Player views top 3 cards of [Library]");
    }

    [Fact]
    public void OpenViewTopXDialog_WhenLibraryHasFewerCards_LogsActualCount()
    {
        var game = CreateGameState();
        AddCardToLibrary(game);

        game.OpenViewTopXDialog(5);

        Assert.Contains(game.GameLog, e => e.Message == "Player views top 1 card of [Library]");
    }

    [Fact]
    public void OpenViewTopXDialog_EmptyLibrary_NoLogEntry()
    {
        var game = CreateGameState();

        game.OpenViewTopXDialog(3);

        Assert.Empty(game.GameLog);
    }

    [Fact]
    public void TransformCard_DoubleFaced_AddsTransformLogEntry()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Delver of Secrets // Insectile Aberration",
            IsDoubleFaced = true,
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });

        game.TransformCard(card);

        Assert.False(card.IsFrontFace);
        Assert.Contains(game.GameLog, e => e.Message == "Player transforms Delver of Secrets to Insectile Aberration");
    }

    [Fact]
    public void TransformCard_NotDoubleFaced_NoLogEntry()
    {
        var game = CreateGameState();
        var card = CreateCardVm();
        card.InitializeFrom(new PlaytestCard
        {
            CardName = "Lightning Bolt",
            IsDoubleFaced = false,
            Zone = GameZone.Battlefield,
            IsFrontFace = true,
        });

        game.TransformCard(card);

        Assert.Empty(game.GameLog);
    }
}
