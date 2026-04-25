using MtgCollectionTracker.ViewModels;
using System;
using System.Reflection;

namespace MtgCollectionTracker.Tests;

public class MainViewModelTests
{
    [Fact]
    public void GetConnectedStatusText_WithConfiguredEndpoint_IncludesTrimmedEndpoint()
    {
        var result = GetConnectedStatusText("http://example.test:5757/");

        Assert.Equal("Connected to server: http://example.test:5757", result);
    }

    [Fact]
    public void GetConnectedStatusText_WithBlankEndpoint_UsesDefaultEndpoint()
    {
        var result = GetConnectedStatusText("   ");

        Assert.Equal("Connected to server: http://localhost:5757", result);
    }

    private static string GetConnectedStatusText(string? remoteServerUrl)
    {
        var method = typeof(MainViewModel).GetMethod("GetConnectedStatusText", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate MainViewModel.GetConnectedStatusText for test.");
        return (string)(method.Invoke(null, [remoteServerUrl]) ?? throw new InvalidOperationException("Expected a status string."));
    }
}
