using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for simple view model data-mapping helpers.
/// </summary>
public class ViewModelTests
{
    [Fact]
    public void ContainerViewModel_WithData_SetsAllProperties()
    {
        var model = new ContainerSummaryModel
        {
            Id = 42,
            Name = "Legacy Binder",
            Description = "All legacy staples",
            Total = 250
        };

        var vm = new ContainerViewModel().WithData(model);

        Assert.Equal(42, vm.Id);
        Assert.Equal("Legacy Binder", vm.Name);
        Assert.Equal("All legacy staples", vm.Description);
        Assert.Equal("Total: 250 cards", vm.Total);
    }

    [Fact]
    public void ContainerViewModel_WithData_NoDescription_LeavesDescriptionNull()
    {
        var model = new ContainerSummaryModel
        {
            Id = 1,
            Name = "Bulk Box",
            Description = null,
            Total = 500
        };

        var vm = new ContainerViewModel().WithData(model);

        Assert.Null(vm.Description);
        Assert.Equal("Total: 500 cards", vm.Total);
    }

    [Fact]
    public void DeckViewModel_WithData_SetsAllProperties()
    {
        var model = new DeckSummaryModel
        {
            Id = 7,
            Name = "Legacy Burn",
            DeckName = "Legacy Burn",
            Format = "Legacy",
            ContainerName = "Deck Box",
            MaindeckTotal = 60,
            SideboardTotal = 15
        };

        var vm = new DeckViewModel().WithData(model);

        Assert.Equal(7, vm.DeckId);
        Assert.Equal("Legacy Burn", vm.Name);
        Assert.Equal("Legacy", vm.Format);
        Assert.Equal("Deck Box", vm.ContainerName);
        Assert.Equal("MD: 60", vm.Maindeck);
        Assert.Equal("SB: 15", vm.Sideboard);
        Assert.True(vm.HasContainer);
    }

    [Fact]
    public void DeckViewModel_WithData_NoContainer_HasContainerIsFalse()
    {
        var model = new DeckSummaryModel
        {
            Id = 3,
            Name = "Pauper Burn",
            DeckName = "Pauper Burn",
            Format = "Pauper",
            ContainerName = null,
            MaindeckTotal = 60,
            SideboardTotal = 0
        };

        var vm = new DeckViewModel().WithData(model);

        Assert.False(vm.HasContainer);
        Assert.Null(vm.ContainerName);
    }

    [Fact]
    public void DeckViewModel_WithData_NoFormat_DisplaysUnknownFormat()
    {
        var model = new DeckSummaryModel
        {
            Id = 5,
            Name = "Casual Deck",
            DeckName = "Casual Deck",
            Format = null,
            MaindeckTotal = 40,
            SideboardTotal = 0
        };

        var vm = new DeckViewModel().WithData(model);

        Assert.Equal("Unknown Format", vm.Format);
    }
}

/// <summary>
/// Tests for <see cref="SendCardsToContainerOrDeckViewModel"/> safeguard logic:
/// when a target deck or container is selected, the corresponding "Un-set" option
/// must be disabled and auto-cleared.
/// </summary>
public class SendCardsToContainerOrDeckViewModelTests
{
    private static SendCardsToContainerOrDeckViewModel CreateViewModel()
    {
        var messenger = new WeakReferenceMessenger();
        var mockService = new Mock<ICollectionTrackingService>();
        var mockClient = new Mock<ScryfallApi.Client.IScryfallApiClient>();
        return new SendCardsToContainerOrDeckViewModel(
            messenger,
            () => new ContainerViewModel(),
            () => new DeckViewModel(),
            mockService.Object,
            mockClient.Object);
    }

    private static DeckViewModel MakeDeck(int id = 1, string deckName = "Test Deck") =>
        new DeckViewModel().WithData(new DeckSummaryModel { Id = id, Name = deckName, DeckName = deckName, Format = "Legacy" });

    private static ContainerViewModel MakeContainer(int id = 1, string containerName = "Test Binder") =>
        new ContainerViewModel().WithData(new ContainerSummaryModel { Id = id, Name = containerName });

    [Fact]
    public void IsUnSetDeckEnabled_IsTrue_WhenNoDeckSelected()
    {
        var vm = CreateViewModel();

        Assert.True(vm.IsUnSetDeckEnabled);
    }

    [Fact]
    public void IsUnSetContainerEnabled_IsTrue_WhenNoContainerSelected()
    {
        var vm = CreateViewModel();

        Assert.True(vm.IsUnSetContainerEnabled);
    }

    [Fact]
    public void IsUnSetDeckEnabled_IsFalse_WhenDeckIsSelected()
    {
        var vm = CreateViewModel();

        vm.SelectedDeck = MakeDeck();

        Assert.False(vm.IsUnSetDeckEnabled);
    }

    [Fact]
    public void IsUnSetDeckEnabled_IsTrue_WhenDeckSelectionCleared()
    {
        var vm = CreateViewModel();
        vm.SelectedDeck = MakeDeck();

        vm.SelectedDeck = null;

        Assert.True(vm.IsUnSetDeckEnabled);
    }

    [Fact]
    public void IsUnSetContainerEnabled_IsFalse_WhenContainerIsSelected()
    {
        var vm = CreateViewModel();

        vm.SelectedContainer = MakeContainer();

        Assert.False(vm.IsUnSetContainerEnabled);
    }

    [Fact]
    public void IsUnSetContainerEnabled_IsTrue_WhenContainerSelectionCleared()
    {
        var vm = CreateViewModel();
        vm.SelectedContainer = MakeContainer();

        vm.SelectedContainer = null;

        Assert.True(vm.IsUnSetContainerEnabled);
    }

    [Fact]
    public void SelectingDeck_ClearsUnSetDeck_WhenItWasChecked()
    {
        var vm = CreateViewModel();
        vm.UnSetDeck = true;

        vm.SelectedDeck = MakeDeck();

        Assert.False(vm.UnSetDeck);
    }

    [Fact]
    public void SelectingDeck_DoesNotAffectUnSetContainer()
    {
        var vm = CreateViewModel();
        vm.UnSetContainer = true;

        vm.SelectedDeck = MakeDeck();

        Assert.True(vm.UnSetContainer);
    }

    [Fact]
    public void SelectingContainer_ClearsUnSetContainer_WhenItWasChecked()
    {
        var vm = CreateViewModel();
        vm.UnSetContainer = true;

        vm.SelectedContainer = MakeContainer();

        Assert.False(vm.UnSetContainer);
    }

    [Fact]
    public void SelectingContainer_DoesNotAffectUnSetDeck()
    {
        var vm = CreateViewModel();
        vm.UnSetDeck = true;

        vm.SelectedContainer = MakeContainer();

        Assert.True(vm.UnSetDeck);
    }

    [Fact]
    public void ClearingDeckSelection_DoesNotRestoreUnSetDeck()
    {
        var vm = CreateViewModel();
        vm.UnSetDeck = true;
        vm.SelectedDeck = MakeDeck(); // clears UnSetDeck

        vm.SelectedDeck = null; // clear selection

        // UnSetDeck remains false after clearing selection (user must re-check manually)
        Assert.False(vm.UnSetDeck);
    }

    [Fact]
    public void ClearingContainerSelection_DoesNotRestoreUnSetContainer()
    {
        var vm = CreateViewModel();
        vm.UnSetContainer = true;
        vm.SelectedContainer = MakeContainer(); // clears UnSetContainer

        vm.SelectedContainer = null; // clear selection

        // UnSetContainer remains false after clearing selection (user must re-check manually)
        Assert.False(vm.UnSetContainer);
    }
}
