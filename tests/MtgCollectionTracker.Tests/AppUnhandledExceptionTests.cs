using System;
using System.Reflection;

namespace MtgCollectionTracker.Tests
{
    public class AppUnhandledExceptionTests
    {
        [Fact]
        public void IsIgnorableUnhandledException_ReturnsTrue_ForKnownDbusServiceUnknownError()
        {
            var exception = new Tmds.DBus.Protocol.DBusException("org.freedesktop.DBus.Error.ServiceUnknown: The name is not activatable");

            Assert.True(IsIgnorable(exception));
        }

        [Fact]
        public void IsIgnorableUnhandledException_ReturnsFalse_ForOtherExceptions()
        {
            Assert.False(IsIgnorable(new InvalidOperationException("boom")));
        }

        private static bool IsIgnorable(Exception exception)
        {
            var method = typeof(MtgCollectionTracker.App).GetMethod("IsIgnorableUnhandledException", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to locate App.IsIgnorableUnhandledException for test.");
            return (bool)(method.Invoke(null, [exception]) ?? throw new InvalidOperationException("Expected a boolean result."));
        }
    }
}

namespace Tmds.DBus.Protocol
{
    internal sealed class DBusException(string message) : Exception(message);
}
