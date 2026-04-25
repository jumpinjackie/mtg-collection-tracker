using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using System;

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
    public void DeckViewModel_WithData_SecondUpdate_RefreshesCommanderStatusAndTooltip()
    {
        var vm = new DeckViewModel().WithData(new DeckSummaryModel
        {
            Id = 10,
            DeckName = "Hakbal",
            Name = "[Commander] Hakbal",
            Format = "Commander",
            IsCommander = true,
            CommanderName = "Hakbal of the Surging Soul",
            IsCommanderValid = false,
            CommanderValidationMessage = "Invalid commander deck: main deck has 98"
        });

        Assert.True(vm.HasCommanderInvalid);
        Assert.False(vm.HasCommanderValid);
        Assert.Equal("Invalid commander deck: main deck has 98", vm.CommanderTooltip);

        vm.WithData(new DeckSummaryModel
        {
            Id = 10,
            DeckName = "Hakbal",
            Name = "[Commander] Hakbal",
            Format = "Commander",
            IsCommander = true,
            CommanderName = "Hakbal of the Surging Soul",
            IsCommanderValid = true,
            CommanderValidationMessage = "Hakbal of the Surging Soul"
        });

        Assert.False(vm.HasCommanderInvalid);
        Assert.True(vm.HasCommanderValid);
        Assert.Equal("Hakbal of the Surging Soul", vm.CommanderTooltip);
    }

    [Fact]
    public void DismantleDeckViewModel_WithDeck_SetsMessageAndLoadsContainers()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        mockService.Setup(s => s.GetContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync([
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
        mockService.Setup(s => s.GetContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync([]);

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
        mockService.Setup(s => s.GetContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync([]);

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
        mockService.Setup(s => s.GetContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(containers);

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
        mockService.Setup(s => s.GetContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(containers);

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
        mockService.Setup(s => s.GetContainersAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync([]);

        // Use a real messenger so that Send<CloseDialogMessage> doesn't throw
        var messenger = new WeakReferenceMessenger();
        var vm = new DismantleDeckViewModel(messenger, mockService.Object, () => new ContainerViewModel());

        // Should complete without throwing
        vm.CancelCommand.Execute(null);
    }
}
