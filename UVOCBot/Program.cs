using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace UVOCBot
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            SetupLogging();
            Log.Information("Appdata stored in " + GetAppdataFilePath(null));

            try
            {
                Log.Information("Starting host");
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
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                })
                .UseSerilog();

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
                directory = Path.Combine(directory, "UVOCBot");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (fileName is not null)
                return Path.Combine(directory, fileName);
            else
                return directory;
        }

        private static void SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("DSharpPlus", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(GetAppdataFilePath("log.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
