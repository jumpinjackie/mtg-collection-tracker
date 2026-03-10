using MtgCollectionTracker.Core.Model;
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
