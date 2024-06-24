namespace MtgCollectionTracker.Data;

public interface IVendorOffer
{
    string Name { get; }

    int AvailableStock { get; }

    decimal Price { get; }
}
