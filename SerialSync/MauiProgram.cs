using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using SerialSync.Misc;
using System.Diagnostics;

namespace SerialSync
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });
			
            builder.Services.AddMudServices(
                 config=>config.SnackbarConfiguration=new SnackbarConfiguration
                 { 
                      PositionClass=Defaults.Classes.Position.BottomLeft,
                       HideTransitionDuration=400
                 }
                );
			builder.Services.AddMauiBlazorWebView();
            //DI日志
            // 获取应用程序的本地存储路径
            string logPath = Settings.LogPath;
            LogSetup.ConfigureNLog(builder.Services);
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

			return builder.Build();
        }
    }
}
