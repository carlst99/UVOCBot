using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using Microsoft.Extensions.Caching.Memory;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Extensions;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Commands;

public class WorldCommands : CommandGroup
{
    private static readonly ValidZoneDefinition[] ValidZones = Enum.GetValues<ValidZoneDefinition>();

    private readonly IInteraction _context;
    private readonly ICensusApiService _censusApi;
    private readonly IPopulationService _populationApi;
    private readonly IMemoryCache _cache;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public WorldCommands
    (
        IInteractionContext context,
        ICensusApiService censusApi,
        IPopulationService populationApi,
        IMemoryCache cache,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _context = context.Interaction;
        _censusApi = censusApi;
        _populationApi = populationApi;
        _cache = cache;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    [Command("default-server")]
    [Description("Sets the default world for planetside-related commands")]
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.ManageGuild, IncludeSelf = false)]
    public async Task<IResult> DefaultWorldCommandAsync(ValidWorldDefinition server)
    {
        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>
        (
            _context.GuildID.Value.Value,
            ct: CancellationToken
        ).ConfigureAwait(false);

        settings.DefaultWorld = (int)server;

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

        return await _feedbackService.SendContextualNeutralAsync(
            $"{Formatter.Emoji("earth_asia")} Your default server has been set to {Formatter.InlineQuote(server.ToString())}",
            ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("population")]
    [Description("Gets the population of a PlanetSide server.")]
    public async Task<IResult> PopulationCommandAsync
    (
        [Description("Set your default server with `/default-server`.")] ValidWorldDefinition server = 0
    )
    {
        if (server == 0)
        {
            Result<ValidWorldDefinition> defaultWorld = await CheckForDefaultServer();
            if (!defaultWorld.IsSuccess)
                return defaultWorld;

            server = defaultWorld.Entity;
        }

        Result<List<EmbedField>> statusFields = await GetStatusEmbedFields(server, false);
        if (!statusFields.IsSuccess)
            return statusFields;

        Result<PopulationDisplayBundle> getPopDisplayInfo = await GetPopulationEmbedFields(server);
        if (!getPopDisplayInfo.IsDefined(out PopulationDisplayBundle popDisplayInfo))
            return getPopDisplayInfo;

        List<EmbedField> fields = new(popDisplayInfo.EmbedFields);
        fields.Add(new EmbedField("Unlocked Continents", Formatter.Bold(" ")));
        fields.AddRange(statusFields.Entity);

        Embed embed = new()
        {
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Title = $"{server} - {popDisplayInfo.TotalPop}",
            Fields = fields,
            Footer = new EmbedFooter($"Source: {popDisplayInfo.Source} | Last updated"),
            Timestamp = popDisplayInfo.Timestamp
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    [Command("status")]
    [Description("Gets the status of a PlanetSide server.")]
    public async Task<IResult> GetServerStatusCommandAsync
    (
        [Description("Set your default server with '/default-server'.")] ValidWorldDefinition server = 0
    )
    {
        if (server == 0)
        {
            Result<ValidWorldDefinition> defaultWorld = await CheckForDefaultServer();
            if (!defaultWorld.IsSuccess)
                return defaultWorld;

            server = defaultWorld.Entity;
        }

        Result<List<EmbedField>> embedFields = await GetStatusEmbedFields(server, true);
        if (!embedFields.IsSuccess)
            return embedFields;

        Embed embed = new()
        {
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Title = server.ToString(),
            Description = "Bar dictates facility ownership.",
            Fields = embedFields.Entity
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    [Command("pop-all")]
    [Description("Gets the population of every server.")]
    public async Task<Result> GetAllPopulationsAsync()
    {
        List<EmbedField> fields = new();

        foreach (ValidWorldDefinition world in Enum.GetValues<ValidWorldDefinition>())
        {
            Result<IPopulation> getPop = await _populationApi.GetWorldPopulationAsync(world, ct: CancellationToken);
            if (!getPop.IsDefined(out IPopulation? pop))
                continue;

            StringBuilder sb = new();
            foreach ((FactionDefinition faction, int pVal) in pop.Population)
            {
                string emoji = faction switch
                {
                    FactionDefinition.NC => "blue_circle",
                    FactionDefinition.TR => "red_circle",
                    FactionDefinition.VS => "purple_circle",
                    FactionDefinition.NSO => "white_circle",
                    _ => "black_circle"
                };
                sb.Append(' ').Append(Formatter.Emoji(emoji)).Append(' ').Append(pVal).Append("");
            }

            fields.Add(new EmbedField
            (
                $"{world} - {pop.Total} ({(DateTimeOffset.UtcNow - pop.Timestamp).TotalMinutes:F1}min ago)",
                sb.ToString()
            ));
        }

        Embed embed = new
        (
            "All Server Populations",
            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR,
            Fields: fields
        );

        return (Result)await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    private async Task<Result<ValidWorldDefinition>> CheckForDefaultServer()
    {
        if (!_context.GuildID.HasValue)
            return new GenericCommandError("To use this command in a DM you must provide a server.");

        PlanetsideSettings? settings = await _dbContext.FindAsync<PlanetsideSettings>
        (
            _context.GuildID.Value.Value,
            CancellationToken
        ).ConfigureAwait(false);

        if (settings?.DefaultWorld is null)
        {
            return new GenericCommandError
            (
                "You haven't set a default server! Please do so using the " +
                $"{Formatter.InlineQuote("/default-server")} command."
            );
        }

        return (ValidWorldDefinition)settings.DefaultWorld;
    }

    private async Task<Result<List<EmbedField>>> GetStatusEmbedFields(ValidWorldDefinition world, bool includeLockedContinents)
    {
        Result<List<Map>> getMapsResult = await _censusApi.GetMapsAsync(world, ValidZones, CancellationToken);

        if (!getMapsResult.IsDefined())
            return Result<List<EmbedField>>.FromError(getMapsResult);

        List<EmbedField> embedFields = new();
        getMapsResult.Entity.Sort
        (
            (m1, m2) => string.CompareOrdinal(m1.ZoneID.Definition.ToString(), m2.ZoneID.Definition.ToString())
        );

        foreach (Map map in getMapsResult.Entity)
        {
            GetMapTerritoryControl(map, out bool isLocked);
            if (isLocked && !includeLockedContinents)
                continue;

            embedFields.Add(GetMapStatusEmbedField(map, world));
        }

        if (embedFields.Count == 0)
            embedFields.Add(new EmbedField("Ruh Roh!", "I can't find any open continents! Something has gone wrong."));

        return embedFields;
    }

    private EmbedField GetMapStatusEmbedField(Map map, ValidWorldDefinition world)
    {
        (double ncPercent, double trPercent, double vsPercent) = GetMapTerritoryControl(map, out bool isLocked);
        string title = GetZoneName(map.ZoneID.Definition);

        object cacheKey = CacheKeyHelpers.GetMetagameEventKey((WorldDefinition)world, map.ZoneID.Definition);
        if (_cache.TryGetValue(cacheKey, out IMetagameEvent? metagameEvent) && metagameEvent!.MetagameEventState is MetagameEventState.Started)
        {
            TimeSpan currentEventDuration = DateTimeOffset.UtcNow - metagameEvent.Timestamp;
            TimeSpan remainingTime = metagameEvent.MetagameEventID.GetAlertDuration() - currentEventDuration;
            title += $@" {Formatter.Emoji("rotating_light")} {remainingTime:%h\h\ %m\m}";
        }
        else if (isLocked)
        {
            title += " " + Formatter.Emoji("lock");
        }

        StringBuilder popBarBuilder = new(1300); // "purple_square" x 9 + some space for formatting
        popBarBuilder.Append("\\> "); // Prevents strange formatting on mobile
        ConstructPopBar(ncPercent, "blue_square", popBarBuilder);
        ConstructPopBar(trPercent, "red_square", popBarBuilder);
        ConstructPopBar(vsPercent, "purple_square", popBarBuilder);

        return new EmbedField(title, popBarBuilder.ToString());
    }

    private static void ConstructPopBar(double percent, string emojiName, StringBuilder sb)
    {
        string name = Formatter.Emoji(emojiName);

        for (int i = 0; i < Math.Round(percent / 10); i++)
            sb.Append(name);
    }

    private async Task<Result<PopulationDisplayBundle>> GetPopulationEmbedFields(ValidWorldDefinition world)
    {
        // We don't return this result if it fails, as the CensusStateWorker will be reporting any retrieval errors
        Result<IPopulation> populationResult = await _populationApi.GetWorldPopulationAsync(world, ct: CancellationToken);
        if (!populationResult.IsDefined(out IPopulation? population))
            return new GenericCommandError("Failed to get population data! This could mean that Honu is down.");

        List<EmbedField> fields = new();
        foreach ((FactionDefinition faction, int popValue) in population.Population)
        {
            if (popValue > 0)
                fields.Add(GetPopulationEmbedField(popValue, population.Total, faction));
        }

        return new PopulationDisplayBundle
        (
            fields,
            population.Total,
            population.Timestamp,
            population.SourceName
        );
    }

    private static EmbedField GetPopulationEmbedField(int factionPopulation, int totalPopulation, FactionDefinition faction)
    {
        string blackSquare = Formatter.Emoji("black_large_square");
        string greenSquare = Formatter.Emoji("green_square");

        string title = faction switch
        {
            FactionDefinition.NC => $"{Formatter.Emoji("blue_circle")} NC - {factionPopulation}",
            FactionDefinition.TR => $"{Formatter.Emoji("red_circle")} TR - {factionPopulation}",
            FactionDefinition.VS => $"{Formatter.Emoji("purple_circle")} VS - {factionPopulation}",
            FactionDefinition.NSO => $"{Formatter.Emoji("white_circle")} NSO - {factionPopulation}",
            _ => $"Unknown - {factionPopulation}"
        };

        // Can't divide by zero!
        if (totalPopulation == 0 || factionPopulation > totalPopulation)
            return new EmbedField(title, blackSquare);

        double tensPercentage = Math.Ceiling((double)factionPopulation / totalPopulation * 10);
        double remainder = 10 - tensPercentage;
        StringBuilder sb = new();

        for (int i = 0; i < tensPercentage; i++)
            sb.Append(greenSquare);

        for (int i = 0; i < remainder; i++)
            sb.Append(blackSquare);

        string stringPercentage = ((double)factionPopulation / totalPopulation * 100).ToString("F1");
        sb.Append(Formatter.Bold("   "));
        sb.Append('(').Append(stringPercentage).Append("%)");

        return new EmbedField(title, sb.ToString());
    }

    private static (double NCPercent, double TRPercent, double VSPercent) GetMapTerritoryControl(Map map, out bool isLocked)
    {
        double regionCount = map.Regions.Row.Count(r => r.RowData.FactionID != FactionDefinition.None);
        double ncPercent = map.Regions.Row.Count(r => r.RowData.FactionID == FactionDefinition.NC) / regionCount * 100;
        double trPercent = map.Regions.Row.Count(r => r.RowData.FactionID == FactionDefinition.TR) / regionCount * 100;
        double vsPercent = map.Regions.Row.Count(r => r.RowData.FactionID == FactionDefinition.VS) / regionCount * 100;

        isLocked = ncPercent > 99 || trPercent > 99 || vsPercent > 99;
        return (ncPercent, trPercent, vsPercent);
    }

    private static string GetZoneName(ZoneDefinition zone)
        => zone switch
        {
            ZoneDefinition.Amerish => $"{Formatter.Emoji("mountain")} {ZoneDefinition.Amerish}",
            ZoneDefinition.Esamir => $"{Formatter.Emoji("snowflake")} {ZoneDefinition.Esamir}",
            ZoneDefinition.Hossin => $"{Formatter.Emoji("deciduous_tree")} {ZoneDefinition.Hossin}",
            ZoneDefinition.Indar => $"{Formatter.Emoji("desert")} {ZoneDefinition.Indar}",
            ZoneDefinition.Koltyr => $"{Formatter.Emoji("radioactive")} {ZoneDefinition.Koltyr}",
            ZoneDefinition.Nexus => $"{Formatter.Emoji("crossed_swords")} {ZoneDefinition.Nexus}",
            ZoneDefinition.Oshur => $"{Formatter.Emoji("ocean")} {ZoneDefinition.Oshur}",
            _ => zone.ToString()
        };

    private readonly record struct PopulationDisplayBundle
    (
        List<EmbedField> EmbedFields,
        int TotalPop,
        DateTimeOffset Timestamp,
        string Source
    );
}
