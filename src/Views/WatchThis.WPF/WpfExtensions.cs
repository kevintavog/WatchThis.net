using System.Windows;
using System.Windows.Interop;

namespace WatchThis.Wpf
{
    static public class WpfExtensions
    {
        public static Rect ScreenDimensions(this Window window)
        {
            var winFormRect = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle).Bounds;
            return new Rect(
                winFormRect.X, winFormRect.Y,
                winFormRect.Width, winFormRect.Height);
        }

    }
}
