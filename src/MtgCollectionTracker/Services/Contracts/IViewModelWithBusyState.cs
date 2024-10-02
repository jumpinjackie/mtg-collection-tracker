using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services.Messaging;
using System;

namespace MtgCollectionTracker.Services.Contracts;

class BusyState : IDisposable
{
    readonly IViewModelWithBusyState _parent;

    public BusyState(IViewModelWithBusyState parent)
    {
        _parent = parent;
        _parent.Messenger.Send(new GlobalBusyMessage(true));
        _parent.IsBusy = true;
    }

    public void Dispose()
    {
        _parent.IsBusy = false;
        _parent.Messenger.Send(new GlobalBusyMessage(false));
    }
}

public interface IViewModelWithBusyState
{
    bool IsBusy { get; set; }

    IMessenger Messenger { get; }

    IDisposable StartBusyState() => new BusyState(this);
}
