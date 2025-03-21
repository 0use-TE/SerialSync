using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialSync.Misc
{
    public static class LogSetup
    {
        public static void ConfigureNLog(IServiceCollection services)
        {
            var config = new NLog.Config.LoggingConfiguration();

            var fileTarget = new NLog.Targets.FileTarget("file")
            {
                FileName = Path.Combine(FileSystem.AppDataDirectory, "SerialSync", "log-.txt"),
                ArchiveFileName = "${basedir}/archives/log-.txt",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                MaxArchiveFiles = 7,
                Layout = "${longdate} [${level:uppercase=true}] ${message}${exception:format=tostring}"
            };
            // 内存目标：只记录用户操作（Information 级别）
            var memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${longdate} [${level:uppercase=true}] ${message}${exception:format=tostring}"
            };

            // 规则：文件记录所有级别，内存只记录 Information
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Error, fileTarget);
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Error, memoryTarget); // 只接收 Information
            NLog.LogManager.Configuration = config;

            // 将 NLog 添加到依赖注入
            services.AddLogging(logging =>
            {
                logging.ClearProviders(); // 移除默认提供程序
                logging.AddNLog();        // 添加 NLog 作为日志提供程序
            });
            services.AddSingleton(memoryTarget);
        }
    }
}
