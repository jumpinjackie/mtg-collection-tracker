using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.Tests;

public class CardsViewModelTests
{
    [Fact]
    public async Task PerformSearch_AfterSummaryRefresh_KeepsFirstTimeMessageHidden()
    {
        // Arrange
        var service = new Mock<ICollectionTrackingService>();
        service.Setup(s => s.GetCollectionSummaryAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new CollectionSummaryModel
        {
            CardTotal = 10,
            ProxyTotal = 0,
            SkuTotal = 10,
        });
        service.Setup(s => s.GetTagsAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync([]);
        service.Setup(s => s.GetCardsAsync(It.IsAny<CardQueryModel>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync([]);

        var viewModel = CreateViewModel(service.Object);
        viewModel.SearchText = "lightning bolt";

        // Act
        await viewModel.PerformSearchCommand.ExecuteAsync(null);
        ApplyTotals(viewModel, new CollectionSummaryModel
        {
            CardTotal = 10,
            ProxyTotal = 0,
            SkuTotal = 10,
        });

        // Assert
        Assert.False(viewModel.ShowFirstTimeMessage);
        Assert.True(viewModel.HasNoResults);
        viewModel.IsActive = false;
    }

    private static CardsViewModel CreateViewModel(ICollectionTrackingService service)
    {
        return new CardsViewModel(
            new WeakReferenceMessenger(),
            service,
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            new Mock<ScryfallApi.Client.IScryfallApiClient>().Object);
    }

    private static void ApplyTotals(CardsViewModel viewModel, CollectionSummaryModel totals)
    {
        var method = typeof(CardsViewModel).GetMethod("ApplyTotals", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate CardsViewModel.ApplyTotals for test.");
        method.Invoke(viewModel, [totals]);
    }
}
