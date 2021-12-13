using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;
using Remora.Results;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using UVOCBot.Commands;
using UVOCBot.Config;
using UVOCBot.Core;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins;
using UVOCBot.Responders;
using UVOCBot.Services;
using UVOCBot.Services.Abstractions;
using UVOCBot.Workers;
#if RELEASE
using Serilog.Core;
#endif

namespace UVOCBot;

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
    public static async Task<int> Main(string[] args)
    {
        try
        {
            IHost host = CreateHostBuilder(args).Build();

            IOptions<GeneralOptions> options = host.Services.GetRequiredService<IOptions<GeneralOptions>>();
            SlashService slashService = host.Services.GetRequiredService<SlashService>();

            IEnumerable<Snowflake> debugServerSnowflakes = options.Value.DebugGuildIds.Select
            (
                l => DiscordSnowflake.New(l)
            );
            Result slashCommandsSupported = slashService.SupportsSlashCommands();

            if (!slashCommandsSupported.IsSuccess)
            {
                Log.Error("The registered commands of the bot aren't supported as slash commands: {reason}", slashCommandsSupported.Error);
            }
            else
            {
#if DEBUG
                foreach (Snowflake guild in debugServerSnowflakes)
                {
                    Result updateSlashCommandsResult = await slashService.UpdateSlashCommandsAsync(guild).ConfigureAwait(false);
                    if (!updateSlashCommandsResult.IsSuccess)
                        Log.Warning("Could not update slash commands for the debug guild {id}: {error}", guild.Value, updateSlashCommandsResult.Error);
                }
#else
                    Result updateSlashCommandsResult = await slashService.UpdateSlashCommandsAsync().ConfigureAwait(false);
                    if (!updateSlashCommandsResult.IsSuccess)
                        Log.Warning("Could not update global application commands");
#endif
            }

            await host.RunAsync().ConfigureAwait(false);
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
            .UseDefaultServiceProvider(s => s.ValidateScopes = true)
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
            .AddDiscordService(s => s.GetRequiredService<IOptions<GeneralOptions>>().Value.BotToken)
            .UseSerilog(logger)
            .UseSystemd()
            .ConfigureServices((c, services) =>
            {
                // Setup configuration bindings
                IConfigurationSection dbConfigSection = c.Configuration.GetSection(nameof(DatabaseOptions));
                DatabaseOptions dbOptions = new();
                dbConfigSection.Bind(dbOptions);

                services.Configure<DatabaseOptions>(dbConfigSection)
                        .Configure<GeneralOptions>(c.Configuration.GetSection(nameof(GeneralOptions)))
                        .Configure<TwitterOptions>(c.Configuration.GetSection(nameof(TwitterOptions)));

                services.AddDbContext<DiscordContext>
                (
                    options =>
                    {
                        options.UseMySql(
                            dbOptions.ConnectionString,
                            new MariaDbServerVersion(new Version(dbOptions.DatabaseVersion))
                        )
#if DEBUG
                            .EnableSensitiveDataLogging()
                            .EnableDetailedErrors()
#endif
                            ;
                    },
                    optionsLifetime: ServiceLifetime.Singleton
                );

                services.AddDbContextFactory<DiscordContext>();

                //Setup API services
                services.AddSingleton<IDbApiService, DbApiService>();

                // Setup other services
                services.AddSingleton(fileSystem)
                    .AddTransient(TwitterClientFactory);

                // Add Discord-related services
                services.AddRemoraServices()
                    .AddCoreDiscordServices()
                    .AddScoped<IAdminLogService, AdminLogService>()
                    .AddScoped<IReplyService, ReplyService>()
                    .AddScoped<IWelcomeMessageService, WelcomeMessageService>()
                    .Configure<CommandResponderOptions>(o => o.Prefix = "<>"); // Sets the text command prefix

                // Plugin registration
                services.AddPlanetsidePlugin(c.Configuration);
                services.AddRolesPlugin();

                services.AddHostedService<GenericWorker>()
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
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
#if DEBUG
        logConfig.MinimumLevel.Debug();
#else
            logConfig.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);

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

    private static IServiceCollection AddRemoraServices(this IServiceCollection services)
    {
        services.Configure<DiscordGatewayClientOptions>(
            o =>
            {
                o.Intents |= GatewayIntents.DirectMessages
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.Guilds
                    | GatewayIntents.GuildMembers;
            });

        services.AddDiscordCommands(true)
                .AddDiscordCaching();

        // services.AddResponder<ComponentInteractionResponder>();
        services.AddResponder<GuildCreateResponder>()
                .AddResponder<GuildMemberResponder>()
                .AddResponder<ReadyResponder>();

        services.AddCommandGroup<AdminCommands>()
                .AddCommandGroup<GeneralCommands>()
                .AddCommandGroup<MovementCommands>()
                .AddCommandGroup<TeamGenerationCommands>()
                .AddCommandGroup<TwitterCommands>()
                .AddCommandGroup<WelcomeMessageCommands>();

        return services;
    }
}
