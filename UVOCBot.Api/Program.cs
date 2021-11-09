using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
#if RELEASE
using Serilog.Core;
using Serilog.Events;
#endif

namespace UVOCBot.Api;

public static class Program
{
    public static int Main(string[] args)
    {
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

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        ILogger? logger = null;

        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((c, _) =>
            {
#if DEBUG
                    logger = SetupLogging();
#else
                    string? seqIngestionEndpoint = c.Configuration.GetSection(nameof(LoggingOptions)).GetSection(nameof(LoggingOptions.SeqIngestionEndpoint)).Value;
                    string? seqApiKey = c.Configuration.GetSection(nameof(LoggingOptions)).GetSection(nameof(LoggingOptions.SeqApiKey)).Value;
                    logger = SetupLogging(seqIngestionEndpoint, seqApiKey);
#endif
                })
            .UseSerilog(logger)
            .UseSystemd()
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }

#if DEBUG
    private static ILogger SetupLogging()
#else
        private static ILogger SetupLogging(string? seqIngestionEndpoint, string? seqApiKey)
#endif
    {
        LoggerConfiguration logConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
#if DEBUG
        logConfig.MinimumLevel.Verbose();
#else
            logConfig.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);

            if (seqIngestionEndpoint is not null)
            {
                LoggingLevelSwitch levelSwitch = new();

                logConfig.MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.Seq(seqIngestionEndpoint, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
            }
            else
            {
                logConfig.MinimumLevel.Information()
                    .WriteTo.File(GetAppdataFilePath("log.log"), LogEventLevel.Warning, "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day);
            }
#endif

        Log.Logger = logConfig.CreateLogger();
        Log.Information("Appdata stored at {path}", GetAppdataFilePath(null));

        return Log.Logger;
    }

    /// <summary>
    /// Gets the path to the specified file, assuming that it is in our appdata store.
    /// </summary>
    /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory.</param>
    /// <remarks>Data is stored in the local appdata.</remarks>
    public static string GetAppdataFilePath(string? fileName)
    {
        string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        directory = Path.Combine(directory, "UVOCBot.Api");

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        if (fileName is not null)
            return Path.Combine(directory, fileName);
        else
            return directory;
    }
}
