using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using System;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for <see cref="CardSkuItemViewModel"/> proxy quantity handling via <see cref="ISendableCardItem"/>.
/// </summary>
public class CardSkuItemViewModelTests
{
    private static CardSkuItemViewModel CreateViewModel()
    {
        var mockService = new Mock<ICollectionTrackingService>();
        var messenger = new WeakReferenceMessenger();
        mockService.Setup(s => s.GetSmallFrontFaceImageAsync(It.IsAny<Guid>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(System.IO.Stream.Null);
        mockService.Setup(s => s.GetSmallBackFaceImageAsync(It.IsAny<Guid>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(System.IO.Stream.Null);
        mockService.Setup(s => s.GetLargeFrontFaceImageAsync(It.IsAny<Guid>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(System.IO.Stream.Null);
        mockService.Setup(s => s.GetLargeBackFaceImageAsync(It.IsAny<Guid>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(System.IO.Stream.Null);
        return new CardSkuItemViewModel(
            mockService.Object,
            messenger,
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            new Mock<ScryfallApi.Client.IScryfallApiClient>().Object);
    }

    [Fact]
    public void SendableCardItem_Quantity_ReturnsProxyQty_WhenSkuIsProxy()
    {
        var vm = CreateViewModel();
        var sku = new CardSkuModel
        {
            Id = Guid.NewGuid(),
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
            Id = Guid.NewGuid(),
            CardName = "Lightning Bolt",
            Edition = "M10",
            Quantity = 4,
            Tags = []
        };

        vm.WithData(sku);

        Assert.Equal(4, ((ISendableCardItem)vm).Quantity);
    }
}
