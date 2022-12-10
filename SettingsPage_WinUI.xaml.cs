namespace MyFaveTimerM7;

public partial class SettingsPage_WinUI : ContentPage
{
    public WindowService WindowService { get; }

    public SettingsPage_WinUI()
	{
		InitializeComponent();

        WindowService = new WindowService();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private void IsLockedWindowOrderToTop_Toggled(object sender, ToggledEventArgs e)
    {
        WindowService.SetWindowLockToTop(App.Current.Windows[0], e.Value);
    }

    private void WindowTransparencySlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        var opacity = (byte)(255 - (byte)e.NewValue);
        WindowService.SetWindowTransparency(App.Current.Windows[0], opacity);
    }
}