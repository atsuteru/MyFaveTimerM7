using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Image = System.Drawing.Image;
using Region = System.Drawing.Region;

namespace MyFaveTimerM7
{
    public partial class WindowService
    {
        public partial void InitializeWindow(Window window, object parameter)
        {
            var imageControl =  (Microsoft.Maui.Controls.Image)parameter;
            var fileImageSource = imageControl.Source as FileImageSource;
            var fileName = fileImageSource.File;

            Bitmap bitmap;
            // Require filename's file exists under Resource/Raw.
            using (Stream stream = FileSystem.OpenAppPackageFileAsync(fileName).Result)
            {
                bitmap = (Bitmap)Image.FromStream(stream);
            }

            GraphicsPath graphicsPath = CalculateControlGraphicsPath(bitmap);
            var reagion = new Region(graphicsPath);

            MauiWinUIWindow winUIWindow = (MauiWinUIWindow)window.Handler.PlatformView;
            IntPtr hwnd = winUIWindow.WindowHandle;
            using (var graphics = Graphics.FromHwnd(hwnd))
            {
                IntPtr hrgn = reagion.GetHrgn(graphics);
                SetWindowRgn(hwnd, hrgn, true);
            }

            // .NET 6だと、Window.Width, Heightがない!!
            window.Width = bitmap.Width;
            window.MinimumWidth = window.Width;
            window.MaximumWidth = window.Width;
            window.Height = bitmap.Height;
            window.MinimumHeight = window.Height;
            window.MaximumHeight = window.Height;

            //TitleBarは,まだどうにもできない！
            //winUIWindow.ExtendsContentIntoTitleBar = true;
            //winUIWindow.SetTitleBar(new Microsoft.UI.Xaml.Controls.Grid() { Height = 100, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x00, 0x00)) });
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
