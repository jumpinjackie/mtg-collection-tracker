using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Tests;

public class ViewTopXViewModelTests
{
    private static PlaytestCardViewModel CreateCard(string name)
    {
        var mockFs = new Mock<ICardImageFileSystem>();
        mockFs.Setup(fileSystem => fileSystem.TryGetStream(It.IsAny<string>(), It.IsAny<string>())).Returns((Stream?)null);

        var mockClient = new Mock<IScryfallApiClient>();
        Func<StrongInject.Owned<MtgCollectionTracker.Data.CardsDbContext>> neverInvoked =
            () => throw new InvalidOperationException("DB should not be accessed in dialog VM tests");

        var cache = new CardImageCache(neverInvoked, mockFs.Object, mockClient.Object);
        var card = new PlaytestCardViewModel(cache);
        card.InitializeFrom(new PlaytestCard
        {
            CardName = name,
            CardType = "Creature",
            Zone = GameZone.Library,
            IsFrontFace = true,
        });

        return card;
    }

    private static ViewTopXViewModel CreateViewModel(
        IReadOnlyList<PlaytestCardViewModel> cards,
        out List<IReadOnlyList<PlaytestCardViewModel>> reorderSnapshots,
        out List<PlaytestCardViewModel> handMoves,
        out List<PlaytestCardViewModel> graveyardMoves,
        out List<PlaytestCardViewModel> exileMoves,
        out List<PlaytestCardViewModel> bottomMoves)
    {
        var reorderSnapshotsLocal = new List<IReadOnlyList<PlaytestCardViewModel>>();
        var handMovesLocal = new List<PlaytestCardViewModel>();
        var graveyardMovesLocal = new List<PlaytestCardViewModel>();
        var exileMovesLocal = new List<PlaytestCardViewModel>();
        var bottomMovesLocal = new List<PlaytestCardViewModel>();

        var messenger = new WeakReferenceMessenger();

        var viewModel = new ViewTopXViewModel(messenger).Configure(
            cards,
            () => { },
            orderedCards => reorderSnapshotsLocal.Add(orderedCards.ToList()),
            card => handMovesLocal.Add(card),
            card => graveyardMovesLocal.Add(card),
            card => exileMovesLocal.Add(card),
            card => bottomMovesLocal.Add(card));

        reorderSnapshots = reorderSnapshotsLocal;
        handMoves = handMovesLocal;
        graveyardMoves = graveyardMovesLocal;
        exileMoves = exileMovesLocal;
        bottomMoves = bottomMovesLocal;
        return viewModel;
    }

    [Fact]
    public void MoveToHand_RemovesSelectedCardAndSelectsNextCard()
    {
        var first = CreateCard("Alpha");
        var second = CreateCard("Beta");
        var third = CreateCard("Gamma");
        var vm = CreateViewModel([first, second, third], out _, out var handMoves, out _, out var exileMoves, out _);

        vm.MoveToHandCommand.Execute(null);
        vm.MoveToExileCommand.Execute(null);

        Assert.Equal([first], handMoves);
        Assert.Equal([second], exileMoves);
        Assert.Equal([third], vm.TopCards);
        Assert.Same(third, vm.SelectedCard);
        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.HasSelection);
    }

    [Fact]
    public void MoveSelectionDown_ReordersVisibleTopCardsAndUpdatesAvailability()
    {
        var first = CreateCard("Alpha");
        var second = CreateCard("Beta");
        var third = CreateCard("Gamma");
        var vm = CreateViewModel([first, second, third], out var reorderSnapshots, out _, out _, out _, out _);

        vm.MoveSelectionDownCommand.Execute(null);

        Assert.Equal([second, first, third], vm.TopCards);
        Assert.Single(reorderSnapshots);
        Assert.Equal([second, first, third], reorderSnapshots[0]);
        Assert.Same(first, vm.SelectedCard);
        Assert.True(vm.CanMoveSelectionUp);
        Assert.True(vm.CanMoveSelectionDown);

        vm.MoveSelectionDownCommand.Execute(null);

        Assert.Equal([second, third, first], vm.TopCards);
        Assert.Equal(2, reorderSnapshots.Count);
        Assert.Equal([second, third, first], reorderSnapshots[1]);
        Assert.True(vm.CanMoveSelectionUp);
        Assert.False(vm.CanMoveSelectionDown);
    }

    [Fact]
    public void MoveToTopOfGraveyard_OnLastVisibleCard_ClearsSelection()
    {
        var onlyCard = CreateCard("Solo");
        var vm = CreateViewModel([onlyCard], out _, out _, out var graveyardMoves, out _, out _);

        vm.MoveToTopOfGraveyardCommand.Execute(null);

        Assert.Equal([onlyCard], graveyardMoves);
        Assert.Empty(vm.TopCards);
        Assert.Null(vm.SelectedCard);
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void ShuffleTop_CanExecuteWithoutSelection()
    {
        var first = CreateCard("Alpha");
        var second = CreateCard("Beta");
        var third = CreateCard("Gamma");
        var vm = CreateViewModel([first, second, third], out var reorderSnapshots, out _, out _, out _, out _);

        vm.SelectedCard = null;

        Assert.True(vm.CanShuffleTop);
        Assert.True(vm.ShuffleTopAndCloseCommand.CanExecute(null));

        vm.ShuffleTopAndCloseCommand.Execute(null);

        Assert.Single(reorderSnapshots);
        Assert.Equal(3, reorderSnapshots[0].Count);
        Assert.True(reorderSnapshots[0].All(vm.TopCards.Contains));
    }
}