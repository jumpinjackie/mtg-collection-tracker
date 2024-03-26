using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;

namespace MtgCollectionTracker.Services;

internal static class MessengerExtensions
{
    public static void ToastNotify(this IMessenger messenger, string message)
    {
        messenger.Send(new NotificationMessage { Content = message });
    }
}
