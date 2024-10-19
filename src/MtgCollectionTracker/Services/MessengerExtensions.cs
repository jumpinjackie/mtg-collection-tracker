using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Messaging;
using System.Linq;

namespace MtgCollectionTracker.Services;

internal static class MessengerExtensions
{
    public static void ToastNotify(this IMessenger messenger, string message, NotificationType type)
    {
        messenger.Send(new NotificationMessage { Content = message, Type = type });
    }

    public static void HandleSkuUpdate(this IMessenger messenger, UpdateCardSkuResult res)
    {
        foreach (var grp in res.ChangedContainer().GroupBy(s => s.OldContainerId))
        {
            var affectedSkus = grp.Select(s => s.Id).ToList();
            messenger.Send(new CardsRemovedFromContainerMessage(grp.Key, affectedSkus));
        }

        foreach (var grp in res.ChangedDecks().GroupBy(s => s.OldDeckId))
        {
            var affectedSkus = grp.Select(s => s.Id).ToList();
            messenger.Send(new CardsRemovedFromDeckMessage(grp.Key, affectedSkus));
        }

        var orphaned = res.Orphaned().Select(s => s.Id).ToList();
        if (orphaned.Count > 0)
        {
            messenger.Send(new CardsOrphanedMessage(orphaned));
        }

        var decksChangedTotals = res.DeckChangedTotals().ToList();
        if (decksChangedTotals.Count > 0)
        {
            messenger.Send(new DeckTotalsChangedMessage(decksChangedTotals));
        }
    }
}
