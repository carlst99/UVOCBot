using DbgCensus.Rest;
using DbgCensus.Rest.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Services;
using Remora.Results;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBot.Commands;
using UVOCBot.Commands.Conditions;
using UVOCBot.Config;
using UVOCBot.Responders;
using UVOCBot.Services;
using UVOCBot.Services.Abstractions;
using UVOCBot.Workers;
#if RELEASE
using Serilog.Core;
#endif

namespace UVOCBot
{
    // Permissions integer: 2570144848
    // - Manage Roles
    // - Manage Channels
    // - Manage Nicknames
    // - View Channels
    // - Send Messages
    // - Embed Links
    // - Read Message History
    // - Add Reactions
    // - Use Slash Commands
    // - Connect
    // - Speak
    // - Move Members
    // OAuth2 URL: https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=2570144848&scope=bot%20applications.commands

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
            ILogger? logger = null;

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((c, _) =>
                {
                    string? seqIngestionEndpoint = c.Configuration.GetSection(nameof(LoggingOptions)).GetSection(nameof(LoggingOptions.SeqIngestionEndpoint)).Value;
                    string? seqApiKey = c.Configuration.GetSection(nameof(LoggingOptions)).GetSection(nameof(LoggingOptions.SeqApiKey)).Value;
#if DEBUG
                    logger = SetupLogging(fileSystem);
#else
                    logger = SetupLogging(fileSystem, seqIngestionEndpoint, seqApiKey);
#endif
                })
                .UseSerilog(logger)
                .UseSystemd()
                .ConfigureServices((c, services) =>
                {
                    // Setup the configuration bindings
                    services.Configure<CensusQueryOptions>(c.Configuration.GetSection(nameof(CensusQueryOptions)))
                            .Configure<GeneralOptions>(c.Configuration.GetSection(nameof(GeneralOptions)))
                            .Configure<TwitterOptions>(c.Configuration.GetSection(nameof(TwitterOptions)));

                    //Setup API services
                    services.AddCensusRestServices()
                            .AddSingleton<ICensusApiService, CensusApiService>()
                            .AddSingleton<IDbApiService, DbApiService>()
                            .AddSingleton<IFisuApiService, FisuApiService>();

                    // Setup other services
                    services.AddSingleton(fileSystem)
                            .AddSingleton<IPermissionChecksService, PermissionChecksService>()
                            .AddSingleton<ISettingsService, SettingsService>()
                            .AddSingleton<IVoiceStateCacheService, VoiceStateCacheService>()
                            .AddSingleton<IWelcomeMessageService, WelcomeMessageService>()
                            .AddTransient(TwitterClientFactory);

                    // Add Discord-related services
                    services.AddDiscordServices()
                            .AddSingleton<IExecutionEventService, ExecutionEventService>()
                            .AddScoped<IPermissionChecksService, PermissionChecksService>()
                            .AddSingleton<MessageResponseHelpers>()
                            .Configure<CommandResponderOptions>((o) => o.Prefix = "<>"); // Sets the text command prefix

                    services.AddHostedService<DiscordService>()
                            .AddHostedService<GenericWorker>()
                            .AddHostedService<TwitterWorker>();
                });
        }

        /// <summary>
        /// Gets the path to the specified file, assuming that it is in our appdata store.
        /// </summary>
        /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory.</param>
        /// <remarks>Data is stored in the local appdata.</remarks>
        public static string GetAppdataFilePath(IFileSystem fileSystem, string? fileName)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            directory = fileSystem.Path.Combine(directory, "UVOCBot");

            if (!fileSystem.Directory.Exists(directory))
                fileSystem.Directory.CreateDirectory(directory);

            if (fileName is not null)
                return fileSystem.Path.Combine(directory, fileName);
            else
                return directory;
        }

#if DEBUG
        private static ILogger SetupLogging(IFileSystem fileSystem)
#else
        private static ILogger SetupLogging(IFileSystem fileSystem, string? seqIngestionEndpoint, string? seqApiKey)
#endif
        {
            LoggerConfiguration logConfig = new LoggerConfiguration()
                .MinimumLevel.Override("System.Net.Http.HttpClient.Discord", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("DaybreakGames.Census", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
#if DEBUG
            logConfig.MinimumLevel.Debug();
#else
            if (seqIngestionEndpoint is not null)
            {
                LoggingLevelSwitch levelSwitch = new();

                logConfig.MinimumLevel.ControlledBy(levelSwitch)
                     .WriteTo.Seq(seqIngestionEndpoint, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
            }
            else
            {
                logConfig.MinimumLevel.Information()
                    .WriteTo.File(
                        GetAppdataFilePath(fileSystem, "log.log"),
                        LogEventLevel.Warning,
                        "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day);
            }
#endif

            Log.Logger = logConfig.CreateLogger();
            Log.Information("Appdata stored at {path}", GetAppdataFilePath(fileSystem, null));

            return Log.Logger;
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

        private static IServiceCollection AddDiscordServices(this IServiceCollection services)
        {
            IOptions<DiscordGatewayClientOptions> gatewayClientOptions = Options.Create(new DiscordGatewayClientOptions
            {
                Intents = GatewayIntents.DirectMessages
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.Guilds
                    | GatewayIntents.GuildVoiceStates
                    | GatewayIntents.GuildMembers
            });
            services.AddSingleton(gatewayClientOptions);

            services.AddDiscordGateway(s => s.GetRequiredService<IOptions<GeneralOptions>>().Value.BotToken)
                    .AddDiscordCommands(false)
                    .AddSingleton<SlashService>()
                    .AddDiscordCaching()
                    .AddHttpClient();

            services.AddResponder<CommandInteractionResponder>()
                    .AddResponder<ComponentInteractionResponder>()
                    .AddResponder<GuildCreateResponder>()
                    .AddResponder<GuildMemberAddResponder>()
                    .AddResponder<ReadyResponder>()
                    .AddResponder<VoiceStateUpdateResponder>();

            services.AddCondition<RequireContextCondition>()
                    .AddCondition<RequireGuildPermissionCondition>();

            services.AddCommandGroup<GeneralCommands>()
                    .AddCommandGroup<GroupCommands>()
                    .AddCommandGroup<MovementCommands>()
                    .AddCommandGroup<RoleCommands>()
                    .AddCommandGroup<PlanetsideCommands>()
                    .AddCommandGroup<TeamGenerationCommands>()
                    .AddCommandGroup<TwitterCommands>()
                    .AddCommandGroup<WelcomeMessageCommands>();

            ServiceProvider serviceProvider = services.BuildServiceProvider(true);
            IOptions<GeneralOptions> options = serviceProvider.GetRequiredService<IOptions<GeneralOptions>>();
            IOptions<CommandResponderOptions> cOptions = serviceProvider.GetRequiredService<IOptions<CommandResponderOptions>>();
            SlashService slashService = serviceProvider.GetRequiredService<SlashService>();

            IEnumerable<Snowflake> debugServerSnowflakes = options.Value.DebugGuildIds.Select(l => new Snowflake(l));
            Result slashCommandsSupported = slashService.SupportsSlashCommands();

            if (!slashCommandsSupported.IsSuccess)
            {
                Log.Error("The registered commands of the bot aren't supported as slash commands: {reason}", slashCommandsSupported.Unwrap().Message);
            }
            else
            {
#if DEBUG
                // Use the following to get rid of troublesome commands

                //IDiscordRestApplicationAPI applicationAPI = serviceProvider.GetRequiredService<IDiscordRestApplicationAPI>();
                //var applicationCommands = applicationAPI.GetGlobalApplicationCommandsAsync(new Snowflake(APPLICATION_CLIENT_ID)).Result;

                //if (applicationCommands.IsSuccess)
                //{
                //    foreach (IApplicationCommand command in applicationCommands.Entity)
                //    {
                //        applicationAPI.DeleteGlobalApplicationCommandAsync(new Snowflake(APPLICATION_CLIENT_ID), command.ID).Wait();
                //    }
                //}

                foreach (Snowflake guild in debugServerSnowflakes)
                {
                    Result updateSlashCommandsResult = slashService.UpdateSlashCommandsAsync(guild).Result;
                    if (!updateSlashCommandsResult.IsSuccess)
                        Log.Warning("Could not update slash commands for the debug guild {id}", guild.Value);
                }
#else
                Result updateSlashCommandsResult = slashService.UpdateSlashCommandsAsync().Result;
                if (!updateSlashCommandsResult.IsSuccess)
                    Log.Warning("Could not update global application commands");
#endif
            }

            return services;
        }
    }
}
