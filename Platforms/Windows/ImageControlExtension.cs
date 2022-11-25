using Microsoft.UI.Input;
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


        public static void SubscribePointerDragMoving(this Image control, Action<PointerPoint> onPointerPressed, Action<PointerPoint> onPointerDragMoved)
        {
            bool isPointerPressed = false;
            DateTime nextMoveTimestamp = default;

            var winuiImage = (Microsoft.UI.Xaml.Controls.Image)control.Handler.PlatformView;
            winuiImage.PointerEntered += (s, e) =>
            {
                isPointerPressed = false;
            };
            winuiImage.PointerExited += (s, e) =>
            {
                isPointerPressed = false;
            };
            winuiImage.PointerPressed += (s, e) =>
            {
                isPointerPressed = true;
                var basePointerPoint = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)s);
                onPointerPressed(basePointerPoint);
            };
            winuiImage.PointerReleased += (s, e) =>
            {
                isPointerPressed = false;
            };
            winuiImage.PointerMoved += (s, e) =>
            {
                if (isPointerPressed && DateTime.UtcNow >= nextMoveTimestamp)
                {
                    var currentPointerPoint = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)s);
                    onPointerDragMoved(currentPointerPoint);
                    nextMoveTimestamp = DateTime.UtcNow.AddMilliseconds(8);
                }
            };
        }
    }
}
