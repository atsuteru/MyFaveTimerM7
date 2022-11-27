using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyFaveTimerM7.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    public static string SemaphoreName { get; } = typeof(WindowService).FullName;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
	{
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        WindowService.AppSemaphore = new Semaphore(1, 1, SemaphoreName, out var createdNew);
        if (!createdNew)
        {
            Exit();
            return;
        }
        base.OnLaunched(args);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

