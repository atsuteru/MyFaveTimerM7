using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Diagnostics;
using Font = Microsoft.Maui.Font;

namespace MyFaveTimerM7;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

    private async void Image_Loaded(object sender, EventArgs e)
	{
        new WindowService().InitializeWindow(App.Current.Windows[0], sender);

        while(true)
        {
            await Task.Delay(10);
            var time = DateTime.Now;
            ClockDate.Text = time.ToString("yyyy/MM/dd ddd");
            ClockTime.Text = time.ToString("HH:mm:ss.ff");
        }
    }

    private async void AlarmSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        // 既にタイムアップしている場合はトグルを戻す
        var time = DateTime.Now;
        var timespan = AlarmTimePicker.Time - new TimeSpan(time.Hour, time.Minute, time.Second);
        if (timespan.TotalSeconds <= 0)
        {
            AlarmSwitch.IsToggled = false;
            return;
        }

        // タイムアップ迄のカウント処理
        while (AlarmSwitch.IsToggled)
        {
            if (timespan.TotalSeconds <= 0)
            {
                AlarmSwitch.IsToggled = false;
                AlarmTime.Text = "00:00:00";
                await Snackbar
                    .Make("Look at the time!", visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Colors.Pink,
                        TextColor = Colors.White,
                        CornerRadius = new CornerRadius(10),
                        Font = Font.SystemFontOfSize(14),
                    }, action: () =>
                    {
                        new WindowService().Activate(App.Current.Windows[0]);
                    })
                    .Show();
                return;
            }
            AlarmTime.Text = timespan.ToString(@"hh\:mm\:ss");
            await Task.Delay(50);

            time = DateTime.Now;
            timespan = AlarmTimePicker.Time - new TimeSpan(time.Hour, time.Minute, time.Second);
        }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}

