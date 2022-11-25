using System.Drawing;
using System.Drawing.Drawing2D;
using Region = System.Drawing.Region;

namespace MyFaveTimerM7.Platforms.Windows
{
    public static class BitmapExtension
    {
        public static Region ToRegion(this Bitmap source)
        {
            using var graphicsPath = CalculateControlGraphicsPath(source);
            return new Region(graphicsPath);
        }

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
