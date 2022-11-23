namespace MyFaveTimerM7;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
        if (count == 0)
        {
            //new WindowService().InitializeWindow(App.Current.Windows[0]);
        }
        count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

    private void Image_Loaded(object sender, EventArgs e)
	{
        new WindowService().InitializeWindow(App.Current.Windows[0], sender);
    }
}

