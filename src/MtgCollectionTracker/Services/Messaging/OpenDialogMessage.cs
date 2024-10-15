using MtgCollectionTracker.ViewModels;
using System;

namespace MtgCollectionTracker.Services.Messaging;

internal class OpenDialogMessage
{
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unused
    /// </summary>
    public int DrawerWidth { get; set; } = 480;

    public required DialogViewModel ViewModel { get; set; }
}
