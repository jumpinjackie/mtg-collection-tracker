using Avalonia.Controls.Notifications;

namespace MtgCollectionTracker.Services.Messaging;

public class NotificationMessage
{
    public required string Content { get; set; }

    public required NotificationType Type { get; set; }
}
