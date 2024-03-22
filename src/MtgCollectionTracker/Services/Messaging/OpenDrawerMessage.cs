using MtgCollectionTracker.ViewModels;

namespace MtgCollectionTracker.Services.Messaging;

internal class OpenDrawerMessage
{
    public int DrawerWidth { get; set; } = 480;

    public required DrawerViewModel ViewModel { get; set; }
}
