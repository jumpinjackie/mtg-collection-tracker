namespace MtgCollectionTracker.Data;

public interface IDeckPrintableSlot
{
    int Quantity { get; }

    string CardName { get; }

    string Edition { get; }

    bool IsLand { get; }

    bool IsSideboard { get; }
}
