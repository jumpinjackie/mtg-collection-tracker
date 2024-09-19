using Avalonia;
using DialogHostAvalonia.Positioners;

namespace MtgCollectionTracker.Services;

public class DialogPopupPositioner : IDialogPopupPositioner
{
    public Rect Update(Size anchorRectangle, Size size)
    {
        double margin = 20;
        Rect posn = new Rect(0, 0, anchorRectangle.Width, anchorRectangle.Height);
        return posn.Inflate(-margin);
    }
}
