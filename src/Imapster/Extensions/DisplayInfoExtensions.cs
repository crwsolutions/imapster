#if WINDOWS
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Windows.Graphics;
#endif
#if MACCATALYST
using UIKit;
#endif
#if IOS || ANDROID
using UIKit;
#endif

namespace Imapster.Extensions;
public static class DisplayInfoExtensions
{
    public static Size GetWorkArea(this DisplayInfo displayInfo)
    {
        double width = displayInfo.Width;
        double height = displayInfo.Height;

        // Subtract the size of system UI elements for each platform.
#if WINDOWS
        if (Microsoft.UI.Xaml.Window.Current is not null)
        {
            IntPtr windowHandle = WindowNative.GetWindowHandle(Microsoft.UI.Xaml.Window.Current.AppWindow.Id);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var bounds = displayArea.WorkArea;
            width = bounds.Width;
            height = bounds.Height;
        }
#elif MACCATALYST
        var screen = UIScreen.MainScreen;
        var bounds = screen.Bounds;
        var safeArea = UIApplication.SharedApplication.Windows[0].SafeAreaInsets;

        width = bounds.Width;
        height = bounds.Height - safeArea.Top - safeArea.Bottom;
#elif IOS
        var bounds = UIScreen.MainScreen.Bounds;
        var safeArea = UIApplication.SharedApplication.KeyWindow?.SafeAreaInsets ?? UIEdgeInsets.Zero;

        width = bounds.Width;
        height = bounds.Height - safeArea.Top - safeArea.Bottom;
#elif ANDROID
        var activity = Platform.CurrentActivity;
        var metrics = new Android.Util.DisplayMetrics();
        activity.WindowManager.DefaultDisplay.GetMetrics(metrics);

        int resourceId = activity.Resources.GetIdentifier("status_bar_height", "dimen", "android");
        int statusBarHeight = resourceId > 0 ? activity.Resources.GetDimensionPixelSize(resourceId) : 0;

        resourceId = activity.Resources.GetIdentifier("navigation_bar_height", "dimen", "android");
        int navigationBarHeight = resourceId > 0 ? activity.Resources.GetDimensionPixelSize(resourceId) : 0;

        width = metrics.WidthPixels / metrics.Density;
        height = (metrics.HeightPixels - statusBarHeight - navigationBarHeight) / metrics.Density;
#endif

        return new Size(width, height);
    }
}
