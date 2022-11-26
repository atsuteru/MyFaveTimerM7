using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using Graphics = System.Drawing.Graphics;
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

        public static Point ConvertPointerToScreen(this Window window, PointerPoint clientPointerPoint)
        {
            IntPtr hwnd = GetWindowHandle(window);
            var baseScreenPoint = new PInvoke.POINT() { x = (int)clientPointerPoint.Position.X, y = (int)clientPointerPoint.Position.Y };
            PInvoke.User32.ClientToScreen(hwnd, ref baseScreenPoint);
            return new Point(baseScreenPoint.x, baseScreenPoint.y);
        }

        #region SetWindowNoBorder
        public static void SetWindowNoBorder(this Window window)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowスタイルの設定
            var style = (PInvoke.User32.SetWindowLongFlags)PInvoke.User32.GetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE);
            style &= ~PInvoke.User32.SetWindowLongFlags.WS_SIZEBOX;     // 左右と下方向のサイズ変更領域の有無（無にすればマウスカーソルが変わらなくなる）
            style &= ~PInvoke.User32.SetWindowLongFlags.WS_BORDER;      // ウィンドウの境界線の有無 (無にすればWin10だと5pxが削れる）
            PInvoke.User32.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE, style);
        }
        #endregion

        #region SetWindowTransparentable
        public static void SetWindowTransparentable(this Window window)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowを透過可能にする
            var exStyle = (PInvoke.User32.SetWindowLongFlags)PInvoke.User32.GetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_EXSTYLE);
            exStyle ^= PInvoke.User32.SetWindowLongFlags.WS_EX_LAYERED;   // ウィンドウを透過可能にする
            PInvoke.User32.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_EXSTYLE, exStyle);
        }
        #endregion

        #region SetWindowRegion
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
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        #endregion

        #region SubscribePointerDragMoving
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
        #endregion

        #region SetWindowTransparent
        public static void SetWindowTransparent(this Window window, byte opacity)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowの透明度を指定する
            SetLayeredWindowAttributes(hwnd, 0, opacity, LayeredWindowFlags.LWA_ALPHA);
        }
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, LayeredWindowFlags dwFlags);
        [Flags]
        private enum LayeredWindowFlags
        {
            LWA_ALPHA = 0x00000002,
            LWA_COLORKEY = 0x00000001,
        }
        #endregion

        #region SetWindowBackground
        public static void SetWindowBackground(this Window window, Microsoft.Maui.Controls.Image imageControl, byte opacity)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // 塗りつぶしに利用するソース画像を取得する
            using var bitmap = imageControl.GetBitmap();

            // ウィンドウの形状をビットマップに合わせて変更
            PInvoke.User32.SetWindowPos(hwnd, 0, 0, 0, bitmap.Width, bitmap.Height, PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE | PInvoke.User32.SetWindowPosFlags.SWP_NOZORDER);
            PInvoke.User32.GetWindowRect(hwnd, out var screenRect);

            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr windowDc = GetDC(hwnd);
            IntPtr memDc = CreateCompatibleDC(screenDc);

            try
            {
                hBitmap = bitmap.GetHbitmap(System.Drawing.Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);
                BitBlt(windowDc, 0, 0, bitmap.Width, bitmap.Height, memDc, 0, 0, TernaryRasterOperations.SRCCOPY | TernaryRasterOperations.CAPTUREBLT); // レイヤードウィンドウではCAPTUREBLTが必要
                SIZE size = new SIZE(bitmap.Width, bitmap.Height);
                PInvoke.POINT pointSource = new PInvoke.POINT() { x = 0, y = 0 };
                PInvoke.POINT topPos = new PInvoke.POINT() { x = screenRect.left, y = screenRect.top };
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = 0; // 0:AC_SRC_OVER
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = opacity;
                blend.AlphaFormat = 1; // 1:AC_SRC_ALPHA
                var ret = UpdateLayeredWindow(hwnd, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, 2);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                ReleaseDC(hwnd, windowDc);
                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
        }
        [DllImport("user32.dll")]
        public extern static IntPtr GetDC(IntPtr handle);
        [DllImport("user32.dll", ExactSpelling = true)]
        public extern static int ReleaseDC(IntPtr handle, IntPtr hDC);
        [DllImport("gdi32.dll")]
        public extern static IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        public extern static bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public extern static IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll")]
        public extern static bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);
        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,
            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,
            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,
            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,
            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,
            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,
            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,
            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,
            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,
            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,
            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,
            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,
            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,
            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,
            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062,
            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows
            /// such as WPF windows with AllowsTransparency="true"
            /// </summary>
            CAPTUREBLT = 0x40000000
        }
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
           ref PInvoke.POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref PInvoke.POINT pptSrc, uint crKey,
           [In] ref BLENDFUNCTION pblend, uint dwFlags);
        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;

            public BLENDFUNCTION(byte op, byte flags, byte alpha, byte format)
            {
                BlendOp = op;
                BlendFlags = flags;
                SourceConstantAlpha = alpha;
                AlphaFormat = format;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }
        #endregion
    }
}
