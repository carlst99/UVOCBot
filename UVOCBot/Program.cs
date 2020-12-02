using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBot.Workers;

namespace UVOCBot
{
    public static class Program
    {
        /// <summary>
        /// The name of the environment variable storing our bot token
        /// </summary>
        private const string BOT_TOKEN_ENV = "UVOCBOT_BOT_TOKEN";
        private const string TWITTER_API_KEY_ENV = "UVOCBOT_TWITTERAPI_KEY";
        private const string TWITTER_API_SECRET_ENV = "UVOCBOT_TWITTERAPI_SECRET";
        private const string TWITTER_API_BEARER_ENV = "UVOCBOT_TWITTERAPI_BEARER_TOKEN";

        public const string PREFIX = "ub!";
        public const string NAME = "UVOCBot";

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
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<DiscordWorker>();
                    services.AddHostedService<TwitterWorker>();
                    services.AddDbContext<BotContext>();
                    services.AddSingleton<ITwitterClient>(new TwitterClient(new ConsumerOnlyCredentials
                    {
                        ConsumerKey = Environment.GetEnvironmentVariable(TWITTER_API_KEY_ENV),
                        ConsumerSecret = Environment.GetEnvironmentVariable(TWITTER_API_SECRET_ENV),
                        BearerToken = Environment.GetEnvironmentVariable(TWITTER_API_BEARER_ENV)
                    }));
                    services.AddSingleton(DiscordClientFactory);
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

            // TODO: Pass a custom IoC container to CommandsNextConfiguration.Services
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
