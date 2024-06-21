namespace MtgCollectionTracker.Core.Model;

public interface IVendorOffer
{
    string Name { get; }

    int AvailableStock { get; }

    decimal Price { get; }
}
