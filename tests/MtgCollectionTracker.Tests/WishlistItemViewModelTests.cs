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
    public async Task WithData_LoadsWishlistImagesThroughService()
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

        await vm.CardImage;
        await vm.CardImageLarge;

        service.Verify(s => s.GetSmallFrontFaceImageAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.GetLargeFrontFaceImageAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }
}
