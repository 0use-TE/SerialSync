using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using SerialSync.Misc;
using Serilog;

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
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // 设置最低日志级别
            .WriteTo.File(
                path: logPath,              // 日志文件路径
                rollingInterval: RollingInterval.Day, // 按天滚动生成新文件
                rollOnFileSizeLimit: true,  // 文件大小超限时滚动
                fileSizeLimitBytes: 10 * 1024 * 1024, // 单个文件最大10MB
                retainedFileCountLimit: 7   // 保留最近7天的日志文件
            )
            .CreateLogger();
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog(dispose: true);
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

			return builder.Build();
        }
    }
}
