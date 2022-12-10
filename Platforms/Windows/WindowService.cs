using MyFaveTimerM7.Platforms.Windows;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;

namespace MyFaveTimerM7
{
    public partial class WindowService
    {
        public partial void InitializeWindow(Window window, object parameter)
        {
            window.Stopped += (s, e) =>
            {
                AppSemaphore.Dispose();
            };

            var imageControl = (Image)parameter;

            // ウィンドウの境界線を消す
            window.SetWindowNoBorder();
            // ウィンドウを透過可能にする
            window.SetWindowTransparentable();
            // ウィンドウの初期透過値を設定
            window.SetWindowTransparent(0xFF);
            // ウィンドウの形状を画像に合わせて切り取る
            SetImageFillToWindow(window, imageControl);
            // ウィンドウの移動を画像のドラッグ移動によってできるようにする
            SetDragMovable(window, imageControl);
        }

        public partial void SetWindowTransparency(Window window, byte opacity)
        {
            window.SetWindowTransparent(opacity);
        }

        public partial void SetWindowLockToTop(Window window, bool isLockToTop)
        {
            window.SetWindowLockToTop(isLockToTop);
        }

        public partial void Activate(Window window)
        {
            window.Activate();
        }

        private static void SetImageFillToWindow(Window window, Microsoft.Maui.Controls.Image imageControl)
        {
            const int WINDOW_FRAME_THICKNESS = 3;
            const int WINDOW_TITLE_SIZEBOX_HEIGHT = 31; // DisableSizeCursor: 8, HideTitleBar: 31
            const int WINDOW_TITLE_HEIGHT = 31;

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
        }

        private static void SetDragMovable(Window window, VisualElement dragHandleElement)
        {
            // 画像上でのドラッグ移動のジェスチャーにより、ウィンドウ自体を移動できるようにする
            window.SetDragMovable(dragHandleElement);
        }
    }
}
