using Microsoft.UI.Xaml.Controls;
using System.Drawing;
using System.Runtime.InteropServices;
using Point = System.Drawing.Point;
using PointerPoint = Microsoft.UI.Input.PointerPoint;
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

        public static Point ConvertPointerToScreen(this Window window, PointerPoint clientPointerPoint)
        {
            IntPtr hwnd = GetWindowHandle(window);
            var baseScreenPoint = new PInvoke.POINT() { x = (int)clientPointerPoint.Position.X, y = (int)clientPointerPoint.Position.Y };
            PInvoke.User32.ClientToScreen(hwnd, ref baseScreenPoint);
            return new Point(baseScreenPoint.x, baseScreenPoint.y);
        }

        public static void SubscribePointerDragMoving(this Window window, Action<PointerPoint> onPointerPressed, Action<PointerPoint> onPointerDragMoved)
        {
            bool isPointerPressed = false;
            DateTime nextMoveTimestamp = default;

            var winuiControl = (Panel)((MauiWinUIWindow)window.Handler.PlatformView).Content;
            winuiControl.PointerEntered += (s, e) =>
            {
                isPointerPressed = false;
            };
            winuiControl.PointerExited += (s, e) =>
            {
                isPointerPressed = false;
            };
            winuiControl.PointerPressed += (s, e) =>
            {
                isPointerPressed = true;
                var basePointerPoint = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)s);
                onPointerPressed(basePointerPoint);
            };
            winuiControl.PointerReleased += (s, e) =>
            {
                isPointerPressed = false;
            };
            winuiControl.PointerMoved += (s, e) =>
            {
                if (isPointerPressed && DateTime.UtcNow >= nextMoveTimestamp)
                {
                    var currentPointerPoint = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)s);
                    onPointerDragMoved(currentPointerPoint);
                    nextMoveTimestamp = DateTime.UtcNow.AddMilliseconds(8);
                }
            };
        }

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
    }
}
