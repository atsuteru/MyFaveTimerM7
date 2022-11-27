using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace MyFaveTimerM7;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("DSEG14Modern-Regular.ttf", "DSEG14ModernRegular");
                fonts.AddFont("DSEG14Modern-Light.ttf", "DSEG14ModernLight");
                fonts.AddFont("DSEG14Modern-Bold.ttf", "DSEG14ModernBold");
                fonts.AddFont("DSEG14Modern-BoldItalic.ttf", "DSEG14ModernBoldItalic");
                fonts.AddFont("DSEG14Modern-Italic.ttf", "DSEG14ModernItalic");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
