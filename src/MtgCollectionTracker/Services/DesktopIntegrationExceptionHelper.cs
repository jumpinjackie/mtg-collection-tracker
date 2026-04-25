using System;

namespace MtgCollectionTracker.Services;

internal static class DesktopIntegrationExceptionHelper
{
    public static bool IsServiceUnavailable(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.Contains("org.freedesktop.DBus.Error.ServiceUnknown", StringComparison.Ordinal)
                || message.Contains("The name is not activatable", StringComparison.Ordinal)
                || message.Contains("org.freedesktop.portal", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
