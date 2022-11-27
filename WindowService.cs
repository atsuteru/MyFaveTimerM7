namespace MyFaveTimerM7
{
    public partial class WindowService
    {
        public partial void InitializeWindow(Window window, object parameter);

        public partial void Activate(Window window);

        public static Semaphore AppSemaphore { get; set; }
    }
}
