using Moq;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using System.IO;
using System.Threading;

namespace MtgCollectionTracker.Tests;

public class WishlistItemViewModelTests
{
    [Fact]
    public async Task WithData_LoadsSmallImageImmediately_AndDeferredLargeImage()
    {
        var service = new Mock<ICollectionTrackingService>();
        service.Setup(s => s.GetSmallFrontFaceImageAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);
        service.Setup(s => s.GetLargeFrontFaceImageAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var vm = new WishlistItemViewModel(service.Object);

        vm.WithData(new WishlistItemModel
        {
            Id = 42,
            CardName = "Black Lotus",
            Edition = "LEA",
            Quantity = 1,
            Tags = [],
            Offers = []
        });

        // Small image loads immediately on WithData
        await vm.CardImage;
        service.Verify(s => s.GetSmallFrontFaceImageAsync(42, It.IsAny<CancellationToken>()), Times.Once);

        // Large image is not loaded yet (deferred until selection)
        service.Verify(s => s.GetLargeFrontFaceImageAsync(42, It.IsAny<CancellationToken>()), Times.Never);

        // After EnsureLargeImageLoaded, the large image starts loading
        vm.EnsureLargeImageLoaded();
        await vm.CardImageLarge;
        service.Verify(s => s.GetLargeFrontFaceImageAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureLargeImageLoaded_CalledMultipleTimes_OnlyLoadsOnce()
    {
        var service = new Mock<ICollectionTrackingService>();
        service.Setup(s => s.GetSmallFrontFaceImageAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);
        service.Setup(s => s.GetLargeFrontFaceImageAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var vm = new WishlistItemViewModel(service.Object);

        vm.WithData(new WishlistItemModel
        {
            Id = 42,
            CardName = "Black Lotus",
            Edition = "LEA",
            Quantity = 1,
            Tags = [],
            Offers = []
        });

        vm.EnsureLargeImageLoaded();
        vm.EnsureLargeImageLoaded();
        vm.EnsureLargeImageLoaded();
        await vm.CardImageLarge;

        // Multiple calls to EnsureLargeImageLoaded should only trigger one load
        service.Verify(s => s.GetLargeFrontFaceImageAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }
}
