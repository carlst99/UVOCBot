using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UVOCBotApi
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            SetupLogging();

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseSystemd()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
#else
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
#endif
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(GetAppdataFilePath("log.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        /// <summary>
        /// Gets the path to the specified file, assuming that it is in our appdata store
        /// </summary>
        /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory</param>
        /// <remarks>Data is stored in the local appdata</remarks>
        /// <returns></returns>
        public static string GetAppdataFilePath(string fileName)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (fileName is not null)
                directory = Path.Combine(directory, "UVOCBotApi");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (fileName is not null)
                return Path.Combine(directory, fileName);
            else
                return directory;
        }
    }
}
