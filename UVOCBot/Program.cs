using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Abstractions.Services;
using UVOCBot.Commands;
using UVOCBot.Config;
using UVOCBot.Core;
using UVOCBot.Discord.Core.Extensions;
using UVOCBot.Plugins;
using UVOCBot.Responders;
using UVOCBot.Services;

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

            IEnumerable<Snowflake> debugServerSnowflakes = options.Value.DebugGuildIds.Select(DiscordSnowflake.New);

            Result slashCommandsSupported = slashService.SupportsSlashCommands();
            if (!slashCommandsSupported.IsSuccess)
            {
                Log.Fatal("The registered commands of the bot aren't supported as slash commands: {Reason}", slashCommandsSupported.Error);
                return 2;
            }

#if DEBUG
            foreach (Snowflake guild in debugServerSnowflakes)
            {
                Result updateSlashCommandsResult = await slashService.UpdateSlashCommandsAsync(guild).ConfigureAwait(false);
                if (updateSlashCommandsResult.IsSuccess)
                    continue;

                Log.Fatal("Could not update slash commands for the debug guild {ID}: {Error}", guild.Value, updateSlashCommandsResult.Error);
                return 2;
            }

            Console.WriteLine("==========> DEBUG");
#else
            //IResult removeOldResult = await RemoveExistingGlobalCommandsAsync(host.Services);
            //if (!removeOldResult.IsSuccess)
            //    return 3;

            Result updateSlashCommandsResult = await slashService.UpdateSlashCommandsAsync().ConfigureAwait(false);
            if (!updateSlashCommandsResult.IsSuccess)
            {
                Log.Fatal("Could not update global application commands: {Error}", updateSlashCommandsResult.Error);
                return 2;
            }

            Console.WriteLine("==========> RELEASE");
#endif

            await host.RunAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Host terminated unexpectedly:");
            Console.WriteLine(ex);

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
            .UseDefaultServiceProvider(s => s.ValidateScopes = true)
            .ConfigureServices((c, _) =>
            {
                string? seqIngestionEndpoint = c.Configuration.GetSection(nameof(LoggingOptions)).GetSection(nameof(LoggingOptions.SeqIngestionEndpoint)).Value;
                string? seqApiKey = c.Configuration.GetSection(nameof(LoggingOptions)).GetSection(nameof(LoggingOptions.SeqApiKey)).Value;
                logger = SetupLogging(seqIngestionEndpoint, seqApiKey);
            })
            .UseSerilog(logger)
            .AddDiscordService(s => s.GetRequiredService<IOptions<GeneralOptions>>().Value.BotToken)
            .UseSystemd()
            .ConfigureServices((c, services) =>
            {
                // Setup configuration bindings
                IConfigurationSection dbConfigSection = c.Configuration.GetSection(nameof(DatabaseOptions));
                DatabaseOptions dbOptions = new();
                dbConfigSection.Bind(dbOptions);

                services.Configure<DatabaseOptions>(dbConfigSection)
                        .Configure<GeneralOptions>(c.Configuration.GetSection(nameof(GeneralOptions)));

                services.AddDbContext<DiscordContext>
                (
                    options =>
                    {
                        options.UseMySql
                        (
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

                // Add Discord-related services
                services.AddRemoraServices()
                        .AddCoreDiscordServices()
                        .AddScoped<IAdminLogService, AdminLogService>();

                // Plugin registration
                services.AddFeedsPlugin(c.Configuration)
                        .AddGreetingsPlugin()
                        .AddPlanetsidePlugin(c.Configuration)
                        .AddRolesPlugin();
            });
    }

    /// <summary>
    /// Gets the path to the specified file, assuming that it is in our appdata store.
    /// </summary>
    /// <param name="fileName">The name of the file stored in the appdata. Leave this parameter null to get the appdata directory.</param>
    /// <remarks>Data is stored in the local appdata.</remarks>
    public static string GetAppdataFilePath(string? fileName)
    {
        string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        directory = Path.Combine(directory, "UVOCBot");

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return fileName is not null
            ? Path.Combine(directory, fileName)
            : directory;
    }

#pragma warning disable RCS1163 // Unused parameter.
    // ReSharper disable twice UnusedParameter.Local
    private static ILogger SetupLogging(string? seqIngestionEndpoint, string? seqApiKey)
#pragma warning restore RCS1163 // Unused parameter.
    {
        LoggerConfiguration logConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Destructure.ByTransforming<ExceptionError>(x => x.Exception)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

#if !DEBUG
        logConfig.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);

        if (!string.IsNullOrEmpty(seqIngestionEndpoint) && !string.IsNullOrEmpty(seqApiKey))
        {
            Serilog.Core.LoggingLevelSwitch levelSwitch = new();

            logConfig.MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.Seq(seqIngestionEndpoint, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
        }
        else
        {
            logConfig.MinimumLevel.Information()
                .WriteTo.File
                (
                    GetAppdataFilePath("log.log"),
                    LogEventLevel.Warning,
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                );
        }
#endif

        Log.Logger = logConfig.CreateLogger();
        Log.Information("Appdata stored at {Path}", GetAppdataFilePath(null));

        return Log.Logger;
    }

    private static IServiceCollection AddRemoraServices(this IServiceCollection services)
    {
        services.Configure<DiscordGatewayClientOptions>
        (
            o =>
            {
                o.Intents |= GatewayIntents.DirectMessages
                             | GatewayIntents.GuildMessages
                             | GatewayIntents.Guilds
                             | GatewayIntents.GuildMembers;
            }
        );

        services.Configure<InteractionResponderOptions>(o => o.SuppressAutomaticResponses = true);

        services.AddDiscordCommands(true, false, false)
                .AddDiscordCaching();

        services.AddResponder<GuildCreateResponder>()
            .AddResponder<GuildMemberResponder>()
            .AddResponder<ReadyResponder>();

        services.AddCommandTree()
                .WithCommandGroup<AdminCommands>()
                .WithCommandGroup<GeneralCommands>()
                .WithCommandGroup<MovementCommands>()
                .WithCommandGroup<TeamGenerationCommands>()
                .Finish();

        return services;
    }

#pragma warning disable RCS1213 // Remove unused member declaration.
    // ReSharper disable once UnusedMember.Local
    private static async Task<IResult> RemoveExistingGlobalCommandsAsync(IServiceProvider services)
    {
        Remora.Discord.API.Abstractions.Rest.IDiscordRestOAuth2API oauth2Api = services.GetRequiredService<Remora.Discord.API.Abstractions.Rest.IDiscordRestOAuth2API>();
        Remora.Discord.API.Abstractions.Rest.IDiscordRestApplicationAPI applicationApi = services.GetRequiredService<Remora.Discord.API.Abstractions.Rest.IDiscordRestApplicationAPI>();

        Result<Remora.Discord.API.Abstractions.Objects.IApplication> appDetails = await oauth2Api.GetCurrentBotApplicationInformationAsync();
        if (!appDetails.IsSuccess)
        {
            Log.Fatal("Could not get application information: {Error}", appDetails.Error);
            return appDetails;
        }

        Result<IReadOnlyList<Remora.Discord.API.Abstractions.Objects.IApplicationCommand>> deleteResult = await applicationApi.BulkOverwriteGlobalApplicationCommandsAsync
        (
            appDetails.Entity.ID,
            new List<Remora.Discord.API.Abstractions.Objects.IBulkApplicationCommandData>()
        );

        if (deleteResult.IsSuccess)
            return Result.FromSuccess();

        Log.Fatal("Could not get delete existing app commands: {Error}", deleteResult.Error);
        return deleteResult;
    }
#pragma warning restore RCS1213 // Remove unused member declaration.
}
