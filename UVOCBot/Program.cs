using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using Serilog;
using Serilog.Events;
using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBot.Config;
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
        public static readonly DiscordColor DEFAULT_EMBED_COLOUR = DiscordColor.Purple;

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
            Serilog.ILogger logger = null;

            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((c, services) =>
                {
                    // Setup the configuration bindings
                    services.Configure<TwitterOptions>(c.Configuration.GetSection(TwitterOptions.ConfigSectionName));
                    services.Configure<GeneralOptions>(c.Configuration.GetSection(GeneralOptions.ConfigSectionName));

                    // Setup the API services
                    services.AddSingleton((s) => RestService.For<IApiService>(
                            s.GetRequiredService<IOptions<GeneralOptions>>().Value.ApiEndpoint));

                    services.AddSingleton(RestService.For<IFisuApiService>("https://ps2.fisu.pw/api"));

                    // Create and setup the filesystem
                    IFileSystem fileSystem = new FileSystem();
                    services.AddSingleton(fileSystem);

                    // Setup Serilog
                    logger = SetupLogging(fileSystem);
                    Log.Information("Appdata stored in " + GetAppdataFilePath(fileSystem, null));

                    // Setup own services
                    //services.AddSingleton<ISettingsService>((s) => new SettingsService(s.GetService<IFileSystem>()));
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IPrefixService, PrefixService>();
                    services.AddSingleton(DiscordClientFactory);
                    services.AddTransient(TwitterClientFactory);

                    // Setup the Census services
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

        private static Serilog.ILogger SetupLogging(IFileSystem fileSystem)
        {
            Serilog.ILogger logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("DSharpPlus", LogEventLevel.Information)
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

        private static DiscordClient DiscordClientFactory(IServiceProvider services)
        {
            GeneralOptions options = services.GetRequiredService<IOptions<GeneralOptions>>().Value;

            DiscordClient client = new DiscordClient(new DiscordConfiguration
            {
                Token = options.BotToken,
                TokenType = TokenType.Bot,
                LoggerFactory = new LoggerFactory().AddSerilog(),
                Intents = DiscordIntents.DirectMessageReactions
                            | DiscordIntents.DirectMessages
                            | DiscordIntents.GuildMessageReactions
                            | DiscordIntents.GuildMessages
                            | DiscordIntents.Guilds
                            | DiscordIntents.GuildVoiceStates
                            | DiscordIntents.GuildMembers
            });

            client.ClientErrored += (_, e) =>
            {
                Log.Error(e.Exception, "The event {event} errored", e.EventName);
                return Task.CompletedTask;
            };

            CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { options.CommandPrefix },
                Services = services,
                PrefixResolver = (m) => CustomPrefixResolver(m, services.GetRequiredService<IPrefixService>())
            });

            commands.CommandErrored += (_, a) => { Log.Error(a.Exception, "Command {command} failed", a.Command); return Task.CompletedTask; };
            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            commands.CommandErrored += async (_, e) =>
            {
                Type exceptionType = e.Exception.GetType();
                if (exceptionType.Equals(typeof(ArgumentException)))
                    await e.Context.RespondAsync($"You haven't provided valid parameters. Please see `{options.CommandPrefix}help` for more information.").ConfigureAwait(false);
                else if (exceptionType.Equals(typeof(TargetInvocationException)))
                    await e.Context.RespondAsync("Oops! Something went wrong while running that command. Please try again.").ConfigureAwait(false);
                else if (exceptionType.Equals(typeof(ChecksFailedException)))
                    await e.Context.RespondAsync("You don't have the necessary permissions to perform this command. Please contact your server administrator/s.").ConfigureAwait(false);
                else if (exceptionType.Equals(typeof(CommandNotFoundException)))
                    await e.Context.RespondAsync($"That command doesn't exist! Please see `{options.CommandPrefix}help` for a list of available commands.").ConfigureAwait(false);
                else
                    await e.Context.RespondAsync("Command failed. Please send this to the developers:\r\n" + e.Exception).ConfigureAwait(false);
            };

            return client;
        }

        private static Task<int> CustomPrefixResolver(DiscordMessage message, IPrefixService prefixService)
        {
            string prefix = prefixService.GetPrefix(message.Channel.GuildId);
            return Task.FromResult(message.GetStringPrefixLength(prefix));
        }
    }
}
