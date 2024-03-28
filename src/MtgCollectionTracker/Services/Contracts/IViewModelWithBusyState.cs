using System;

namespace MtgCollectionTracker.Services.Contracts;

class BusyState : IDisposable
{
    readonly IViewModelWithBusyState _parent;

    public BusyState(IViewModelWithBusyState parent)
    {
        _parent = parent;
        _parent.IsBusy = true;
    }

    public void Dispose()
    {
        _parent.IsBusy = false;
    }
}

public interface IViewModelWithBusyState
{
    bool IsBusy { get; set; }

    IDisposable StartBusyState() => new BusyState(this);
}
