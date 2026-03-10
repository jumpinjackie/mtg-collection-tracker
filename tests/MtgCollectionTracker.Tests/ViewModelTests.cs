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

    [Fact]
    public void DismantleDeckViewModel_WithDeck_SetsMessageAndLoadsContainers()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainers()).Returns([
            new ContainerSummaryModel { Id = 1, Name = "Main Binder" },
            new ContainerSummaryModel { Id = 2, Name = "Shoe Box" }
        ]);

        var messenger = new WeakReferenceMessenger();
        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());

        vm.WithDeck(42, "Legacy Burn", _ => ValueTask.CompletedTask);

        Assert.Equal("Are you sure you want to dismantle (Legacy Burn)?", vm.Message);
        Assert.NotNull(vm.AvailableContainers);
        // 2 real containers + 1 sentinel "(Unparented)" prepended
        Assert.Equal(3, vm.AvailableContainers.Count());
        Assert.Equal("(Unparented)", vm.AvailableContainers.First().Name);
        Assert.Contains(vm.AvailableContainers, c => c.Name == "Main Binder");
        Assert.Contains(vm.AvailableContainers, c => c.Name == "Shoe Box");
        // (Unparented) is pre-selected
        Assert.NotNull(vm.SelectedContainer);
        Assert.Equal("(Unparented)", vm.SelectedContainer.Name);
        Assert.Equal(0, vm.SelectedContainer.Id);
    }

    [Fact]
    public void DismantleDeckViewModel_WithDeck_NoContainers_OnlyUnparentedSentinel()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainers()).Returns([]);

        var messenger = new WeakReferenceMessenger();
        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());

        vm.WithDeck(1, "Deck With No Containers", _ => ValueTask.CompletedTask);

        Assert.NotNull(vm.AvailableContainers);
        // Only the sentinel "(Unparented)" entry
        Assert.Single(vm.AvailableContainers);
        Assert.Equal("(Unparented)", vm.AvailableContainers.Single().Name);
    }

    [Fact]
    public async Task DismantleDeckViewModel_Confirm_InvokesCallbackWithNullContainerWhenUnparentedSelected()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainers()).Returns([]);

        var messenger = new WeakReferenceMessenger();
        int? capturedContainerId = -1; // sentinel: -1 means callback was not called

        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());
        vm.WithDeck(1, "My Deck", containerId =>
        {
            capturedContainerId = containerId;
            return ValueTask.CompletedTask;
        });

        // SelectedContainer is the "(Unparented)" sentinel (Id=0) by default
        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Null(capturedContainerId);
    }

    [Fact]
    public async Task DismantleDeckViewModel_Confirm_InvokesCallbackWithSelectedContainerId()
    {
        var containers = new List<ContainerSummaryModel>
        {
            new() { Id = 5, Name = "Target Box" }
        };

        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainers()).Returns(containers);

        var messenger = new WeakReferenceMessenger();
        int? capturedContainerId = -1;

        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());
        vm.WithDeck(1, "My Deck", containerId =>
        {
            capturedContainerId = containerId;
            return ValueTask.CompletedTask;
        });

        // Select the real container (skip the "(Unparented)" sentinel at index 0)
        vm.SelectedContainer = vm.AvailableContainers!.First(c => c.Id > 0);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal(5, capturedContainerId);
    }

    [Fact]
    public async Task DismantleDeckViewModel_Confirm_AfterSelectingContainer_CanSwitchBackToUnparented()
    {
        var containers = new List<ContainerSummaryModel>
        {
            new() { Id = 3, Name = "Some Box" }
        };

        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainers()).Returns(containers);

        var messenger = new WeakReferenceMessenger();
        int? capturedContainerId = -1;

        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());
        vm.WithDeck(1, "My Deck", containerId =>
        {
            capturedContainerId = containerId;
            return ValueTask.CompletedTask;
        });

        // First pick a real container, then switch back to the "(Unparented)" sentinel
        vm.SelectedContainer = vm.AvailableContainers!.First(c => c.Id > 0);
        vm.SelectedContainer = vm.AvailableContainers!.First(c => c.Id == 0);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Null(capturedContainerId);
    }

    [Fact]
    public void DismantleDeckViewModel_Cancel_ExecutesWithoutError()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainers()).Returns([]);

        // Use a real messenger so that Send<CloseDialogMessage> doesn't throw
        var messenger = new WeakReferenceMessenger();
        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());

        // Should complete without throwing
        vm.CancelCommand.Execute(null);
    }
}

/// <summary>
/// Tests for <see cref="CardSkuItemViewModel"/> proxy quantity handling via <see cref="ISendableCardItem"/>.
/// </summary>
public class CardSkuItemViewModelTests
{
    private static CardSkuItemViewModel CreateViewModel()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetSmallFrontFaceImageAsync(It.IsAny<string>())).ReturnsAsync(System.IO.Stream.Null);
        mockService.Setup(s => s.GetSmallBackFaceImageAsync(It.IsAny<string>())).ReturnsAsync(System.IO.Stream.Null);
        mockService.Setup(s => s.GetLargeFrontFaceImageAsync(It.IsAny<string>())).ReturnsAsync(System.IO.Stream.Null);
        mockService.Setup(s => s.GetLargeBackFaceImageAsync(It.IsAny<string>())).ReturnsAsync(System.IO.Stream.Null);
        return new CardSkuItemViewModel(mockService.Object);
    }

    [Fact]
    public void SendableCardItem_Quantity_ReturnsProxyQty_WhenSkuIsProxy()
    {
        var vm = CreateViewModel();
        var sku = new CardSkuModel
        {
            Id = 1,
            CardName = "Black Lotus",
            Edition = "PROXY",
            Quantity = 4,
            Tags = []
        };

        vm.WithData(sku);

        Assert.Equal(4, ((ISendableCardItem)vm).Quantity);
    }

    [Fact]
    public void SendableCardItem_Quantity_ReturnsRealQty_WhenSkuIsNotProxy()
    {
        var vm = CreateViewModel();
        var sku = new CardSkuModel
        {
            Id = 2,
            CardName = "Lightning Bolt",
            Edition = "M10",
            Quantity = 4,
            Tags = []
        };

        vm.WithData(sku);

        Assert.Equal(4, ((ISendableCardItem)vm).Quantity);
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
