using CommunityToolkit.Mvvm.Messaging;
using Moq;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.ViewModels;
using ScryfallApi.Client;

namespace MtgCollectionTracker.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void Constructor_WithRemoteClientMode_SetsIsRemoteClientMode()
    {
        var vm = CreateViewModel(new AppSettings { Mode = AppMode.RemoteClient });

        Assert.True(vm.IsRemoteClientMode);
    }

    [Fact]
    public void Constructor_WithLocalMode_ClearsIsRemoteClientMode()
    {
        var vm = CreateViewModel(new AppSettings { Mode = AppMode.Local });

        Assert.False(vm.IsRemoteClientMode);
    }

    private static SettingsViewModel CreateViewModel(AppSettings appSettings)
    {
        var service = new Mock<ICollectionTrackingService>();
        var client = new Mock<IScryfallApiClient>();
        var messenger = new WeakReferenceMessenger();
        return new SettingsViewModel(service.Object, client.Object, messenger, appSettings);
    }
}
