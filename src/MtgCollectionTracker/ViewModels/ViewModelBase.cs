using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace MtgCollectionTracker.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected void ThrowIfNotDesignMode()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
            throw new System.Exception("This constructor was meant to be invoked only by the Avalonia designer, not by dependency injection or manually");
    }
}

public abstract class RecipientViewModelBase : ObservableRecipient
{
    protected RecipientViewModelBase() : base() { }

    protected RecipientViewModelBase(IMessenger messenger) : base(messenger) { }

    protected void ThrowIfNotDesignMode()
    {
        if (!Avalonia.Controls.Design.IsDesignMode)
            throw new System.Exception("This constructor was meant to be invoked only by the Avalonia designer, not by dependency injection or manually");
    }
}
