using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
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

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            IHost host = CreateHostBuilder(args).Build();

            IOptions<GeneralOptions> options = host.Services.GetRequiredService<IOptions<GeneralOptions>>();
            SlashService slashService = host.Services.GetRequiredService<SlashService>();

            IEnumerable<Snowflake> debugServerSnowflakes = options.Value.DebugGuildIds.Select(DiscordSnowflake.New);

#if DEBUG
            foreach (Snowflake guild in debugServerSnowflakes)
            {
                Result updateSlashCommandsResult = await slashService.UpdateSlashCommandsAsync(guild).ConfigureAwait(false);
                if (updateSlashCommandsResult.IsSuccess)
                    continue;

                Log.Fatal("Could not update slash commands for the debug guild {ID}: {Error}", guild.Value, updateSlashCommandsResult.Error);
                return;
            }
#else
            //IResult removeOldResult = await RemoveExistingGlobalCommandsAsync(host.Services);
            //if (!removeOldResult.IsSuccess)
            //    return;

            Result updateSlashCommandsResult = await slashService.UpdateSlashCommandsAsync().ConfigureAwait(false);
            if (!updateSlashCommandsResult.IsSuccess)
            {
                Log.Fatal("Could not update global application commands: {Error}", updateSlashCommandsResult.Error);
                return;
            }
#endif

            using IServiceScope scope = host.Services.CreateScope();
            await using DiscordContext dbContext = scope.ServiceProvider.GetRequiredService<DiscordContext>();
            await dbContext.Database.MigrateAsync();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            // Just in case the logger hasn't been configured yet
            Console.WriteLine("Host terminated unexpectedly:");
            Console.WriteLine(ex);

            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static HostApplicationBuilder CreateHostBuilder(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSystemd();

        LoggingOptions? logOptions = builder.Configuration.GetSection(LoggingOptions.CONFIG_NAME)
            .Get<LoggingOptions>();
        SetupLogging(logOptions?.SeqIngestionEndpoint, logOptions?.SeqApiKey);
        builder.Services.AddSerilog();

            // #if DEBUG // Used for EF core migrations
            // .ConfigureAppConfiguration((c, builder) =>
            // {
            //     builder.AddConfiguration(c.Configuration)
            //         .AddUserSecrets<Program>();
            // })
            // #endif

        // Setup configuration bindings
        DatabaseOptions dbOptions = builder.Configuration.GetRequiredSection(DatabaseOptions.CONFIG_NAME)
            .Get<DatabaseOptions>()!;

        builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.CONFIG_NAME))
                .Configure<GeneralOptions>(builder.Configuration.GetSection(nameof(GeneralOptions)));

        // Set up the database
        void DbOptionsBuilder(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(dbOptions.ConnectionString, b => b.MigrationsAssembly("UVOCBot"))
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                .EnableDetailedErrors(builder.Environment.IsDevelopment());
        }

        builder.Services.AddDbContext<DiscordContext>(DbOptionsBuilder, optionsLifetime: ServiceLifetime.Singleton)
            .AddDbContextFactory<DiscordContext>(DbOptionsBuilder);

        // Add Discord-related services
        AddRemoraServices(builder.Services)
            .AddCoreDiscordServices()
            .AddScoped<IAdminLogService, AdminLogService>();

        // Plugin registration
        builder.Services.AddApexLegendsPlugin(builder.Configuration)
                .AddFeedsPlugin(builder.Configuration)
                .AddGreetingsPlugin()
                .AddPlanetsidePlugin(builder.Configuration)
                .AddRolesPlugin()
                .AddSpaceEngineersPlugin();

        return builder;
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

    // ReSharper disable twice UnusedParameter.Local
    private static void SetupLogging(string? seqIngestionEndpoint, string? seqApiKey)
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
#endif

        Log.Logger = logConfig.CreateLogger();
        Log.Information("Appdata stored at {Path}", GetAppdataFilePath(null));
    }

    private static IServiceCollection AddRemoraServices(IServiceCollection services)
    {
        services.AddDiscordService(s => s.GetRequiredService<IOptions<GeneralOptions>>().Value.BotToken);

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

    // ReSharper disable once UnusedMember.Local
    private static async Task<IResult> RemoveExistingGlobalCommandsAsync(IServiceProvider services)
    {
        IDiscordRestOAuth2API oauth2Api = services.GetRequiredService<IDiscordRestOAuth2API>();
        IDiscordRestApplicationAPI applicationApi = services.GetRequiredService<IDiscordRestApplicationAPI>();

        Result<IApplication> appDetails = await oauth2Api.GetCurrentBotApplicationInformationAsync();
        if (!appDetails.IsSuccess)
        {
            Log.Fatal("Could not get application information: {Error}", appDetails.Error);
            return appDetails;
        }

        Result<IReadOnlyList<IApplicationCommand>> deleteResult = await applicationApi.BulkOverwriteGlobalApplicationCommandsAsync
        (
            appDetails.Entity.ID,
            new List<IBulkApplicationCommandData>()
        );

        if (deleteResult.IsSuccess)
            return Result.FromSuccess();

        Log.Fatal("Could not get delete existing app commands: {Error}", deleteResult.Error);
        return deleteResult;
    }
}
