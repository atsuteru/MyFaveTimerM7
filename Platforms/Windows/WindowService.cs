using MyFaveTimerM7.Platforms.Windows;
using System.Drawing;

namespace MyFaveTimerM7
{
    public partial class WindowService
    {
        public partial void InitializeWindow(Window window, object parameter)
        {
            var imageControl = (Microsoft.Maui.Controls.Image)parameter;

            SetWindowStyle(window);

            SetImageFillToWindow(window, imageControl);

            SetDragMovable(window, imageControl);
        }

        private static void SetWindowStyle(Window window)
        {
            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // Windowスタイルの設定
            var style = (PInvoke.User32.SetWindowLongFlags)PInvoke.User32.GetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE);
            PInvoke.User32.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE,
                style
                & ~PInvoke.User32.SetWindowLongFlags.WS_SIZEBOX // 左右と下方向のサイズ変更領域の有無（無にすればマウスカーソルが変わらなくなる）
                & ~PInvoke.User32.SetWindowLongFlags.WS_BORDER // ウィンドウの境界線の有無 (無にすればWin10だと5pxが削れる）
                );
        }

        private static void SetImageFillToWindow(Window window, Microsoft.Maui.Controls.Image imageControl)
        {
            const int WINDOW_FRAME_THICKNESS = 3;
            const int WINDOW_TITLE_SIZEBOX_HEIGHT = 31; // DisableSizeCursor: 8, HideTitleBar: 31
            const int WINDOW_TITLE_HEIGHT = 31;

            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // 塗りつぶしに利用するソース画像を取得する
            using var sourceBitmap = imageControl.GetBitmap();

            // 下地となる透明な画像を作る。サイズは元画像の両側にウィンドウフレームの幅を足したサイズで。
            using var layerdBitMap = new Bitmap(sourceBitmap.Width + WINDOW_FRAME_THICKNESS * 2, sourceBitmap.Height + WINDOW_TITLE_SIZEBOX_HEIGHT);

            // 透明な背景に画像を重ねる
            using (var g = Graphics.FromImage(layerdBitMap))
            {
                g.DrawImage(sourceBitmap, WINDOW_FRAME_THICKNESS, WINDOW_TITLE_SIZEBOX_HEIGHT, sourceBitmap.Width, sourceBitmap.Height);
            }
            var reagion = layerdBitMap.ToRegion();

            // 透明でない部分だけを抜き出したリージョンをウィンドウの表示領域として設定する
            window.SetWindowRegion(reagion);

            // Imageコントロールのサイズが画像と同じピクセルになるようにウィンドウサイズを固定
            // ※.NET 6だと、Window.Width, Heightがないので .NET7必須。
            window.Width = sourceBitmap.Width + WINDOW_FRAME_THICKNESS * 2;
            window.Height = sourceBitmap.Height + WINDOW_TITLE_HEIGHT + WINDOW_FRAME_THICKNESS;

            // 画像をタイトルバー部分に重ねる
            imageControl.Margin = new Thickness(
                0,
                -(WINDOW_TITLE_HEIGHT - WINDOW_TITLE_SIZEBOX_HEIGHT),
                0,
                WINDOW_TITLE_HEIGHT - WINDOW_TITLE_SIZEBOX_HEIGHT);

            //これでタイトルバーの高さをちっちゃくして、ボタンも無効化できる→それならタイトルエリアをリージョンで削り取る方がまし。
            //いずれにせよ全体のドラッグ＆ドロップで移動できなければ話にならない
            //winUIWindow.ExtendsContentIntoTitleBar = true;
            //winUIWindow.SetTitleBar(new Microsoft.UI.Xaml.Controls.Grid());
        }

        private static void SetDragMovable(Window window, Microsoft.Maui.Controls.Image imageControl)
        {
            PInvoke.POINT baseScreenPoint = default;
            double baseWindowPositionX = 0d;
            double baseWindowPositionY = 0d;

            // Windowハンドルの取得
            IntPtr hwnd = window.GetWindowHandle();

            // 画像上でのドラッグ移動にあわせて、ウィンドウ自体を移動させる
            imageControl.SubscribePointerDragMoving(
                onPointerPressed: (pointerPoint) =>
                {
                    baseScreenPoint = new PInvoke.POINT() { x = (int)pointerPoint.Position.X, y = (int)pointerPoint.Position.Y };
                    PInvoke.User32.ClientToScreen(hwnd, ref baseScreenPoint);
                    baseWindowPositionX = window.X;
                    baseWindowPositionY = window.Y;
                },
                onPointerDragMoved: (pointerPoint) =>
                {
                    var currentScreenPoint = new PInvoke.POINT() { x = (int)pointerPoint.Position.X, y = (int)pointerPoint.Position.Y };
                    PInvoke.User32.ClientToScreen(hwnd, ref currentScreenPoint);
                    var screenPointOffsetX = currentScreenPoint.x - baseScreenPoint.x;
                    var screenPointOffsetY = currentScreenPoint.y - baseScreenPoint.y;
                    window.X = baseWindowPositionX + screenPointOffsetX;
                    window.Y = baseWindowPositionY + screenPointOffsetY;
                    //差は感じない
                    //PInvoke.User32.SetWindowPos(hwnd, PInvoke.User32.SpecialWindowHandles.HWND_TOP,
                    //    baseWindowPositionX + screenPointOffsetX,
                    //    baseWindowPositionY + screenPointOffsetY,
                    //    0, 0,
                    //    PInvoke.User32.SetWindowPosFlags.SWP_NOSIZE);
                });
        }
    }
}
