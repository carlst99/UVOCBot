using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;
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
using System.Drawing;
using System.IO.Abstractions;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBotRemora.Commands;
using UVOCBotRemora.Config;
using UVOCBotRemora.Responders;
using UVOCBotRemora.Services;
using UVOCBotRemora.Workers;

namespace UVOCBotRemora
{
    // Permissions integer: 2435927120
    // - Manage Roles
    // - Manage Channels
    // - View Channels
    // - Send Messages
    // - Embed Links
    // - Read Message History
    // - Add Reactions
    // - Use Slash Commands
    // - Connect
    // - Speak
    // - Move Members
    // OAuth2 URL: https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=2435927120&scope=bot%20applications.commands

    public static class Program
    {
        public static readonly Color DEFAULT_EMBED_COLOUR = Color.Purple;

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

            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .UseSerilog(logger)
                .ConfigureServices((c, services) =>
                {
                    // Setup the configuration bindings
                    services.Configure<TwitterOptions>(c.Configuration.GetSection(TwitterOptions.ConfigSectionName));
                    services.Configure<GeneralOptions>(c.Configuration.GetSection(GeneralOptions.ConfigSectionName));

                    //Setup API services
                    services.AddSingleton((s) => RestService.For<IAPIService>(
                            s.GetRequiredService<IOptions<GeneralOptions>>().Value.ApiEndpoint));
                    services.AddSingleton((s) => RestService.For<IFisuApiService>(
                            s.GetRequiredService<IOptions<GeneralOptions>>().Value.FisuApiEndpoint));

                    services.AddSingleton(fileSystem)
                            .AddSingleton<ISettingsService, SettingsService>()
                            .AddSingleton<IVoiceStateCacheService, VoiceStateCacheService>()
                            .AddTransient(TwitterClientFactory);

                    // Add Discord-related services
                    services.AddDiscordServices()
                            .AddSingleton<IPrefixService, PrefixService>()
                            .AddSingleton<MessageResponseHelpers>()
                            .AddSingleton<IExecutionEventService, ExecutionEventService>()
                            .Configure<CommandResponderOptions>((o) => o.Prefix = "<>"); // Sets the text command prefix

                    // Setup the Daybreak Census services
                    GeneralOptions generalOptions = services.BuildServiceProvider().GetRequiredService<IOptions<GeneralOptions>>().Value;
                    services.AddCensusServices(options =>
                        options.CensusServiceId = generalOptions.CensusApiKey);

                    services.AddHostedService<DiscordService>()
                            .AddHostedService<TwitterWorker>();
                    //services.AddHostedService<PlanetsideWorker>();
                });
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
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("System.Net.Http.HttpClient.Discord", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("DaybreakGames.Census", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(GetAppdataFilePath(fileSystem, "log.log"), LogEventLevel.Warning, "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day)
                .CreateLogger();

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
                Intents = GatewayIntents.DirectMessageReactions
                    | GatewayIntents.DirectMessages
                    | GatewayIntents.GuildMessageReactions
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.Guilds
                    | GatewayIntents.GuildVoiceStates
                    | GatewayIntents.GuildMembers
            });
            services.AddSingleton(gatewayClientOptions);

            services.AddDiscordGateway(s => s.GetRequiredService<IOptions<GeneralOptions>>().Value.BotToken)
                    .AddDiscordCommands(true)
                    .AddDiscordCaching()
                    .AddHttpClient();

            services.AddResponder<GuildCreateResponder>()
                    .AddResponder<ReadyResponder>()
                    .AddResponder<VoiceStateUpdateResponder>();

            // Add commands
            services.AddCommandGroup<GeneralCommands>()
                    .AddCommandGroup<GroupCommands>()
                    .AddCommandGroup<MovementCommands>()
                    .AddCommandGroup<RoleCommands>()
                    .AddCommandGroup<PlanetsideCommands>()
                    .AddCommandGroup<TeamGenerationCommands>();

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
