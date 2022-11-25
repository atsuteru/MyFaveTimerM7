using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Windows.Devices.Input;
using Image = System.Drawing.Image;
using Region = System.Drawing.Region;

namespace MyFaveTimerM7
{
    public partial class WindowService
    {
        public partial void InitializeWindow(Window window, object parameter)
        {
            const int WINDOW_FRAME_THICKNESS = 3;
            const int WINDOW_TITLE_SIZEBOX_HEIGHT = 8;
            const int WINDOW_TITLE_HEIGHT = 31;

            var imageControl =  (Microsoft.Maui.Controls.Image)parameter;
            var fileImageSource = imageControl.Source as FileImageSource;
            var fileName = fileImageSource.File;

            // WinUIでは、画像は Resource/Raw 配下に MauiAsset として登録されている必要がある。
            using var sourceStream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
            using var sourceBitmap = (Bitmap)Image.FromStream(sourceStream);

            // 下地となる透明な画像を作る。サイズは元画像の両側にウィンドウフレームの幅を足したサイズで。
            var layerdBitMap = new Bitmap(sourceBitmap.Width + WINDOW_FRAME_THICKNESS * 2, sourceBitmap.Height + WINDOW_TITLE_SIZEBOX_HEIGHT);

            // 透明な背景に画像を重ねる
            using (var g = Graphics.FromImage(layerdBitMap))
            {
                g.DrawImage(sourceBitmap, WINDOW_FRAME_THICKNESS, WINDOW_TITLE_SIZEBOX_HEIGHT, sourceBitmap.Width, sourceBitmap.Height);
            }

            // 背景画像から、透明でない部分だけを抜き出したリージョンを作る
            using GraphicsPath graphicsPath = CalculateControlGraphicsPath(layerdBitMap);
            var reagion = new Region(graphicsPath);

            // 透明でない部分だけを抜き出したリージョンをウィンドウの表示領域として設定する
            MauiWinUIWindow winUIWindow = (MauiWinUIWindow)window.Handler.PlatformView;
            IntPtr hwnd = winUIWindow.WindowHandle;
            using (var graphics = Graphics.FromHwnd(hwnd))
            {
                IntPtr hrgn = reagion.GetHrgn(graphics);
                SetWindowRgn(hwnd, hrgn, true);
            }

            var style = (PInvoke.User32.SetWindowLongFlags)PInvoke.User32.GetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE);
            PInvoke.User32.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_STYLE, 
                style 
                & ~PInvoke.User32.SetWindowLongFlags.WS_SIZEBOX // 左右と下方向のサイズ変更領域の有無（無にすればマウスカーソルが変わらなくなる）
                & ~PInvoke.User32.SetWindowLongFlags.WS_BORDER // ウィンドウの境界線の有無 (無にすればWin10だと5pxが削れる）
                );

            // Imageコントロールのサイズが画像と同じピクセルになるようにウィンドウサイズを固定
            // ※.NET 6だと、Window.Width, Heightがないので .NET7必須。
            window.Width = sourceBitmap.Width + WINDOW_FRAME_THICKNESS * 2;
            window.MinimumWidth = window.Width;
            window.MaximumWidth = window.Width;
            window.Height = sourceBitmap.Height + WINDOW_TITLE_HEIGHT + WINDOW_FRAME_THICKNESS;
            window.MinimumHeight = window.Height;
            window.MaximumHeight = window.Height;

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

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        // https://www.codeproject.com/Articles/6048/Creating-Bitmap-Regions-for-Forms-and-Buttons
        private static GraphicsPath CalculateControlGraphicsPath(Bitmap bitmap)
        {
            // Create GraphicsPath for our bitmap calculation
            GraphicsPath graphicsPath = new GraphicsPath();

            // Use the top left pixel as our transparent color
            System.Drawing.Color colorTransparent = bitmap.GetPixel(0, 0);

            // This is to store the column value where an opaque pixel is first found.
            // This value will determine where we start scanning for trailing 
            // opaque pixels.
            int colOpaquePixel = 0;

            // Go through all rows (Y axis)
            for (int row = 0; row < bitmap.Height; row++)
            {
                // Reset value
                colOpaquePixel = 0;

                // Go through all columns (X axis)
                for (int col = 0; col < bitmap.Width; col++)
                {
                    // If this is an opaque pixel, mark it and search 
                    // for anymore trailing behind
                    if (bitmap.GetPixel(col, row) != colorTransparent)
                    {
                        // Opaque pixel found, mark current position
                        colOpaquePixel = col;

                        // Create another variable to set the current pixel position
                        int colNext = col;

                        // Starting from current found opaque pixel, search for 
                        // anymore opaque pixels trailing behind, until a transparent
                        // pixel is found or minimum width is reached
                        for (colNext = colOpaquePixel; colNext < bitmap.Width; colNext++)
                            if (bitmap.GetPixel(colNext, row) == colorTransparent)
                                break;

                        // Form a rectangle for line of opaque pixels found and 
                        // add it to our graphics path
                        graphicsPath.AddRectangle(new Rectangle(colOpaquePixel,
                                                   row, colNext - colOpaquePixel, 1));

                        // No need to scan the line of opaque pixels just found
                        col = colNext;
                    }
                }
            }

            // Return calculated graphics path
            return graphicsPath;
        }
    }
}
