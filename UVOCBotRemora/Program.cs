using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Services;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBotRemora.Config;
using UVOCBotRemora.Responders;
using UVOCBotRemora.Services;
using UVOCBotRemora.Workers;

namespace UVOCBotRemora
{
    // Permissions integer: 285281344
    // - Manage Roles
    // - View Channels
    // - Send Messages
    // - Read Message History
    // - Add Reactions
    // - Move Members
    // OAuth2 URL: https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=285281344&scope=bot

    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
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
            IFileSystem fileSystem = new FileSystem();
            ILogger logger = SetupLogging(fileSystem);
            Log.Information("Appdata stored in " + GetAppdataFilePath(fileSystem, null));

            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((c, services) =>
                {
                    // Setup the configuration bindings
                    services.Configure<TwitterOptions>(c.Configuration.GetSection(TwitterOptions.ConfigSectionName));
                    services.Configure<GeneralOptions>(c.Configuration.GetSection(GeneralOptions.ConfigSectionName));

                    // Setup API services
                    services.AddSingleton((s) => RestService.For<IApiService>(
                            s.GetRequiredService<IOptions<GeneralOptions>>().Value.ApiEndpoint));
                    services.AddSingleton((s) => RestService.For<IFisuApiService>(
                            s.GetRequiredService<IOptions<GeneralOptions>>().Value.FisuApiEndpoint));

                    // Setup the Discord gateway client
                    IOptions<DiscordGatewayClientOptions> gatewayClientOptions = Options.Create(new DiscordGatewayClientOptions
                    {
                        Intents = GatewayIntents.DirectMessageReactions
                            | GatewayIntents.DirectMessages
                            | GatewayIntents.GuildMessageReactions
                            | GatewayIntents.GuildMessages
                            | GatewayIntents.Guilds
                            | GatewayIntents.GuildVoiceStates
                            | GatewayIntents.GuildMembers
                    });
                    services.AddSingleton(gatewayClientOptions);

                    //ResponderService responderService = new();
                    services.AddDiscordGateway(s => s.GetRequiredService<IOptions<GeneralOptions>>().Value.BotToken)
                            .AddDiscordCommands(true)
                            .AddResponder<ReadyResponder>();
                            //.AddSingleton<IResponderTypeRepository>(responderService);

                    // Setup own services
                    services.AddSingleton(fileSystem);
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IPrefixService, PrefixService>();
                    services.AddTransient(TwitterClientFactory);

                    // Setup the Daybreak Census services
                    GeneralOptions generalOptions = services.BuildServiceProvider().GetRequiredService<IOptions<GeneralOptions>>().Value;
                    services.AddCensusServices(options =>
                        options.CensusServiceId = generalOptions.CensusApiKey);

                    services.AddHostedService<DiscordWorker>();
                    services.AddHostedService<TwitterWorker>();
                    //services.AddHostedService<PlanetsideWorker>();
                })
                .UseSerilog(logger);
        }

        /// <summary>
        /// Gets the path to the specified file, assuming that it is in our appdata store
        /// </summary>
        /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory</param>
        /// <remarks>Data is stored in the local appdata</remarks>
        /// <returns></returns>
        public static string GetAppdataFilePath(IFileSystem fileSystem, string? fileName)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (fileName is not null)
                directory = fileSystem.Path.Combine(directory, "UVOCBot");

            if (!fileSystem.Directory.Exists(directory))
                fileSystem.Directory.CreateDirectory(directory);

            if (fileName is not null)
                return fileSystem.Path.Combine(directory, fileName);
            else
                return directory;
        }

        private static ILogger SetupLogging(IFileSystem fileSystem)
        {
            ILogger logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("DaybreakGames.Census", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(GetAppdataFilePath(fileSystem, "log.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Logger = logger;
            return logger;
        }

        private static ITwitterClient TwitterClientFactory(IServiceProvider services)
        {
            TwitterOptions options = services.GetRequiredService<IOptions<TwitterOptions>>().Value;

            return new TwitterClient(new ConsumerOnlyCredentials
            {
                ConsumerKey = options.Key,
                ConsumerSecret = options.Secret,
                BearerToken = options.BearerToken
            });
        }
    }
}
