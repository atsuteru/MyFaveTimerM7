using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using static PInvoke.User32;
using Graphics = System.Drawing.Graphics;
using Point = System.Drawing.Point;
using PointerPoint = Microsoft.UI.Input.PointerPoint;
using Region = System.Drawing.Region;
using Window = Microsoft.Maui.Controls.Window;

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
            ClientToScreen(hwnd, ref baseScreenPoint);
            return new Point(baseScreenPoint.x, baseScreenPoint.y);
        }

        public static void Activate(this Window window)
        {
            MauiWinUIWindow winUIWindow = (MauiWinUIWindow)window.Handler.PlatformView;
            winUIWindow.Activate();
        }

        #region SetWindowNoBorder
        public static void SetWindowNoBorder(this Window window)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowスタイルの設定
            var style = (SetWindowLongFlags)GetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE);
            style &= ~SetWindowLongFlags.WS_SIZEBOX;     // 左右と下方向のサイズ変更領域の有無（無にすればマウスカーソルが変わらなくなる）
            style &= ~SetWindowLongFlags.WS_BORDER;      // ウィンドウの境界線の有無 (無にすればWin10だと5pxが削れる）
            SetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE, style);
        }
        #endregion

        #region SetWindowTransparentable
        public static void SetWindowTransparentable(this Window window)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowを透過可能にする
            var exStyle = (SetWindowLongFlags)GetWindowLong(hwnd, WindowLongIndexFlags.GWL_EXSTYLE);
            exStyle ^= SetWindowLongFlags.WS_EX_LAYERED;   // ウィンドウを透過可能にする
            SetWindowLong(hwnd, WindowLongIndexFlags.GWL_EXSTYLE, exStyle);
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
        public static void SetDragMovable(this Window window, VisualElement dragHandleElement)
        {
            bool isPointerPressed = false;
            SafeCursorHandle previousCursorHandle = null;
            IntPtr hwnd = GetWindowHandle(window);

            var winuiElement = (UIElement)dragHandleElement.Handler.PlatformView;
            winuiElement.PointerPressed += (s, e) =>
            {
                isPointerPressed = true;
            };
            winuiElement.PointerReleased += (s, e) =>
            {
                // Reset Cursor
                if (previousCursorHandle != null)
                {
                    SetCursor(previousCursorHandle);
                    previousCursorHandle = null;
                }
                isPointerPressed = false;
            };
            winuiElement.PointerMoved += (s, e) =>
            {
                // エレメント上をクリックしたまま「なぞった」場合
                if (isPointerPressed)
                {
                    // カーソルを「移動：の形状に変更
                    previousCursorHandle = SetCursor(LoadCursor(IntPtr.Zero, 32646/*IDC_SIZEALL*/));

                    // タイトルバーがクリックされたことにする → ウィンドウの移動ができる
                    // https://www.codeproject.com/Articles/11114/Move-window-form-without-Titlebar-in-C
                    ReleaseCapture();
                    SendMessage(hwnd, WindowMessage.WM_NCLBUTTONDOWN, 0x2/*HT_CAPTION*/, IntPtr.Zero);
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
            SetWindowPos(hwnd, 0, 0, 0, bitmap.Width, bitmap.Height, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOZORDER);
            GetWindowRect(hwnd, out var screenRect);

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

        #region SetWindowLockToTop
        public static void SetWindowLockToTop(this Window window, bool isLockToTop)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowの最前面設定／最前面解除
            SetWindowPos(hwnd, 
                isLockToTop ? SpecialWindowHandles.HWND_TOPMOST : SpecialWindowHandles.HWND_NOTOPMOST,
                0, 0, 0, 0, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
        }
        #endregion
    }
}
