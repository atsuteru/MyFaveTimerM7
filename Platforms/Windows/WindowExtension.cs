using System.Drawing;
using System.Runtime.InteropServices;
using Region = System.Drawing.Region;

namespace MyFaveTimerM7.Platforms.Windows
{
    public static class WindowExtension
    {
        public static IntPtr GetWindowHandle(this Window window)
        {
            MauiWinUIWindow winUIWindow = (MauiWinUIWindow)window.Handler.PlatformView;
            return winUIWindow.WindowHandle;
        }

        public static void SetWindowRegion(this Window window, Region region)
        {
            IntPtr hwnd = GetWindowHandle(window);
            using (var graphics = Graphics.FromHwnd(hwnd))
            {
                IntPtr hrgn = region.GetHrgn(graphics);
                SetWindowRgn(hwnd, hrgn, true);
            }
        }

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
    }
}
