using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;

namespace UVOCBot
{
    public static class Program
    {
        // Permissions integer: 268504128
        // - Manage Roles
        // - Send Messages
        // - Read Message History
        // - Add Reactions
        // - View Channels
        // OAuth2 URL: https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=268504128&scope=bot

        #region Constants

        /// <summary>
        /// The name of the environment variable storing our bot token
        /// </summary>
        private const string TOKEN_ENV_NAME = "UVOC_BOT_TOKEN";

        public const string PREFIX = "ub!";
        public const string NAME = "UVOCBot";

        #endregion

        private static readonly ManualResetEvent _exitMRE = new ManualResetEvent(false);

        public static DiscordClient Client { get; private set; }

        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            ILoggerFactory logger = SetupLogging();
            // Useful for debugging
            Log.Information("Appdata stored in " + GetAppdataFilePath(null));

            // Connect to the Discord API
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable(TOKEN_ENV_NAME, EnvironmentVariableTarget.Process),
                TokenType = TokenType.Bot,
                LoggerFactory = logger,
                Intents = DiscordIntents.DirectMessageReactions
                | DiscordIntents.DirectMessages
                | DiscordIntents.GuildMessageReactions
                | DiscordIntents.GuildMessages
                | DiscordIntents.Guilds
                | DiscordIntents.GuildVoiceStates
            });

            // Setup the DI
            IServiceProvider services = SetupServiceProvider();

            // TODO: Pass a custom IoC container to CommandsNextConfiguration.Services
            CommandsNextExtension commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { PREFIX },
                Services = services
            });
            commands.CommandErrored += (_, a) => { Log.Error(a.Exception, "Command {command} failed", a.Command); return Task.CompletedTask; };
            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            await Client.ConnectAsync(new DiscordActivity(PREFIX + "help", ActivityType.ListeningTo)).ConfigureAwait(false);

            // Begin any scheduled tasks we have
            JobManager.Initialize(new JobRegistry());
            JobManager.JobException += info => Log.Error(info.Exception, "An error occured in the job {name}", info.Name);

            // Clean up when a shutdown is requested by the user
            Console.CancelKeyPress += Console_CancelKeyPress;

            _exitMRE.WaitOne();
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
                directory = Path.Combine(directory, "UVOCBot");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (fileName is not null)
                return Path.Combine(directory, fileName);
            else
                return directory;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            JobManager.Stop();
            Client.DisconnectAsync().Wait();
            _exitMRE.Set();
        }

        private static ILoggerFactory SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
                .MinimumLevel.Override("DSharpPlus", Serilog.Events.LogEventLevel.Information)
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.Console()
                .CreateLogger();

            return new LoggerFactory().AddSerilog();
        }

        private static IServiceProvider SetupServiceProvider()
        {
            string apiKey = Environment.GetEnvironmentVariable("TWITTER_API_KEY");
            string apiSecret = Environment.GetEnvironmentVariable("TWITTER_API_SECRET");
            string bearerToken = Environment.GetEnvironmentVariable("TWITTER_BEARER_TOKEN");

            return new ServiceCollection()
                .AddDbContext<BotContext>()
                .AddSingleton<ITwitterClient>(new TwitterClient(apiKey, apiSecret, bearerToken))
                .BuildServiceProvider();
        }
    }
}
