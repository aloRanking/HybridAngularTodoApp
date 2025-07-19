using CommunityToolkit.Maui;
using HybridAngularTodoApp.Services;
using Microsoft.Extensions.Logging;

namespace HybridAngularTodoApp;

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
			});
			string dbPath = Path.Combine(FileSystem.AppDataDirectory, "todo-ang.db");
        builder.Services.AddSingleton(new TodoDatabase(dbPath));

#if DEBUG
		builder.Logging.AddDebug();
		builder.Services.AddHybridWebViewDeveloperTools();
#endif

		return builder.Build();
	}
}

