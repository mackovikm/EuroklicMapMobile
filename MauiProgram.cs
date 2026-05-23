using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using EuroklicMapMobile.Services;
using EuroklicMapMobile.ViewModels;
using EuroklicMapMobile.Views;

namespace EuroklicMapMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        // Podržení labelu se třídou CopyOnLongPress → zkopíruje celý text do schránky
#if ANDROID
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("CopyOnLongPress", (handler, view) =>
        {
            if (view is Label label && label.StyleClass?.Contains("CopyOnLongPress") == true)
            {
                handler.PlatformView.LongClickable = true;
                handler.PlatformView.LongClick += (_, _) =>
                {
                    var text = label.Text;
                    if (!string.IsNullOrEmpty(text))
                        MainThread.BeginInvokeOnMainThread(async () =>
                            await Clipboard.Default.SetTextAsync(text));
                };
            }
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<HttpClient>(_ =>
            new HttpClient { Timeout = TimeSpan.FromSeconds(30) });

        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();

        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<MapPage>();

        var app = builder.Build();

        // Nakonfiguruj API s pevne zakodovanymi hodnotami
        var apiService = app.Services.GetRequiredService<IApiService>();
        apiService.Configure(AppConstants.ApiBaseUrl, AppConstants.ApiKey);

        return app;
    }
}
