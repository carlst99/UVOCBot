using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refit;
using Serilog;
using Serilog.Events;
using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBot.Services;
using UVOCBot.Workers;

namespace UVOCBot
{
    // Permissions integer: 268504128
    // - Manage Roles
    // - Send Messages
    // - Read Message History
    // - Add Reactions
    // - View Channels
    // OAuth2 URL: https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=268504128&scope=bot

    public static class Program
    {
        /// <summary>
        /// The name of the environment variable storing our bot token
        /// </summary>
        private const string BOT_TOKEN_ENV = "UVOCBOT_BOT_TOKEN";
        private const string TWITTER_API_KEY_ENV = "UVOCBOT_TWITTERAPI_KEY";
        private const string TWITTER_API_SECRET_ENV = "UVOCBOT_TWITTERAPI_SECRET";
        private const string TWITTER_API_BEARER_ENV = "UVOCBOT_TWITTERAPI_BEARER_TOKEN";
        private const string API_ENDPOINT_ENV = "UVOCBOT_API_ENDPOINT";

        public const string PREFIX = "ub!";
        public const string NAME = "UVOCBot";

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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseSystemd()
                .ConfigureServices((_, services) =>
                {
                    IFileSystem fileSystem = new FileSystem();
                    services.AddSingleton(fileSystem);

                    // Setup Serilog
                    SetupLogging(fileSystem);
                    Log.Information("Appdata stored in " + GetAppdataFilePath(fileSystem, null));

                    services.AddSingleton<ISettingsService>((s) => new SettingsService(s.GetService<IFileSystem>()));
                    services.AddSingleton(DiscordClientFactory);
                    services.AddTransient(TwitterClientFactory);
                    services.AddSingleton(RestService.For<IApiService>(Environment.GetEnvironmentVariable(API_ENDPOINT_ENV)));
                    services.AddHostedService<DiscordWorker>();
                    services.AddHostedService<TwitterWorker>();
                });

        /// <summary>
        /// Gets the path to the specified file, assuming that it is in our appdata store
        /// </summary>
        /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory</param>
        /// <remarks>Data is stored in the local appdata</remarks>
        /// <returns></returns>
        public static string GetAppdataFilePath(IFileSystem fileSystem, string fileName)
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

        private static void SetupLogging(IFileSystem fileSystem)
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
                .WriteTo.File(GetAppdataFilePath(fileSystem, "log.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        private static ITwitterClient TwitterClientFactory(IServiceProvider services)
        {
            return new TwitterClient(new ConsumerOnlyCredentials
            {
                ConsumerKey = Environment.GetEnvironmentVariable(TWITTER_API_KEY_ENV),
                ConsumerSecret = Environment.GetEnvironmentVariable(TWITTER_API_SECRET_ENV),
                BearerToken = Environment.GetEnvironmentVariable(TWITTER_API_BEARER_ENV)
            });
        }

        private static DiscordClient DiscordClientFactory(IServiceProvider services)
        {
            DiscordClient client = new DiscordClient(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable(BOT_TOKEN_ENV, EnvironmentVariableTarget.Process),
                TokenType = TokenType.Bot,
                LoggerFactory = new LoggerFactory().AddSerilog(),
                Intents = DiscordIntents.DirectMessageReactions
                            | DiscordIntents.DirectMessages
                            | DiscordIntents.GuildMessageReactions
                            | DiscordIntents.GuildMessages
                            | DiscordIntents.Guilds
                            | DiscordIntents.GuildVoiceStates
            });

            CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { PREFIX },
                Services = services
            });
            commands.CommandErrored += (_, a) => { Log.Error(a.Exception, "Command {command} failed", a.Command); return Task.CompletedTask; };
            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            return client;
        }
    }
}
