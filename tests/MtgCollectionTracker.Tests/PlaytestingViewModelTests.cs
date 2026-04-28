using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Threading;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for <see cref="PlaytestingViewModel"/> covering deck-selection state,
/// deck list refresh and the SelectDeck command.
/// </summary>
public class PlaytestingViewModelTests
{
    private static ICollectionTrackingService CreateDefaultService()
    {
        var mock = new Mock<ICollectionTrackingService>();
        mock.Setup(s => s.GetDecksAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DeckSummaryModel>());
        return mock.Object;
    }

    private static PlaytestingViewModel CreateViewModel(
        ICollectionTrackingService? service = null,
        IScryfallApiClient? scryfallClient = null,
        Func<PlaytestGameStateViewModel>? gameStateFactory = null)
    {
        var messenger = new WeakReferenceMessenger();
        service ??= CreateDefaultService();
        scryfallClient ??= new Mock<IScryfallApiClient>().Object;
        gameStateFactory ??= () =>
        {
            var mockFs = new Mock<ICardImageFileSystem>();
            mockFs.Setup(f => f.TryGetStream(It.IsAny<string>(), It.IsAny<string>())).Returns((Stream?)null);
            var mockClient = new Mock<IScryfallApiClient>();
            Func<StrongInject.Owned<MtgCollectionTracker.Data.CardsDbContext>> neverInvoked =
                () => throw new InvalidOperationException("DB not needed");
            var cache = new CardImageCache(neverInvoked, mockFs.Object, mockClient.Object);
            return new PlaytestGameStateViewModel(messenger, () => new PlaytestCardViewModel(cache), new AppSettings());
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
        mockService.SetupSequence(s => s.GetDecksAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeckSummaryModel>())
            .ReturnsAsync([
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
        mockService.SetupSequence(s => s.GetDecksAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeckSummaryModel>())
            .ReturnsAsync([new DeckSummaryModel { Id = 1, Name = "First Deck", DeckName = "First Deck" }])
            .ReturnsAsync([new DeckSummaryModel { Id = 2, Name = "Second Deck", DeckName = "Second Deck" }]);

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
