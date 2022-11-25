using Bitmap = System.Drawing.Bitmap;

namespace MyFaveTimerM7.Platforms.Windows
{
    public static class ImageControlExtension
    {
        public static Bitmap GetBitmap(this Image control)
        {
            var fileImageSource = control.Source as FileImageSource;
            var fileName = fileImageSource.File;
            using var sourceStream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
            return (Bitmap)System.Drawing.Image.FromStream(sourceStream);
        }
    }
}
