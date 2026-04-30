using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MtgCollectionTracker.ViewModels;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that supports replacing its entire contents
/// with a single <see cref="NotifyCollectionChangedAction.Reset"/> notification, avoiding
/// the per-item UI update overhead of calling <see cref="ObservableCollection{T}.Add"/> in a loop.
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Clears the collection and replaces it with <paramref name="items"/>,
    /// raising a single <see cref="NotifyCollectionChangedAction.Reset"/> event
    /// instead of one event per added item.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
