using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using Microsoft.Extensions.Caching.Memory;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Commands;

public class WorldCommands : CommandGroup
{
    private static readonly ValidZoneDefinition[] ValidZones = Enum.GetValues<ValidZoneDefinition>();

    private readonly ICommandContext _context;
    private readonly ICensusApiService _censusApi;
    private readonly IPopulationService _populationApi;
    private readonly IMemoryCache _cache;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public WorldCommands(
        ICommandContext context,
        ICensusApiService censusApi,
        IPopulationService fisuApi,
        IMemoryCache cache,
        DiscordContext dbContext,
        FeedbackService feedbackService)
    {
        _context = context;
        _censusApi = censusApi;
        _populationApi = fisuApi;
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
        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

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
        if (server != 0)
            return await SendWorldPopulationAsync(server).ConfigureAwait(false);

        if (!_context.GuildID.HasValue)
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                "To use this command in a DM you must provide a server.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

        if (settings.DefaultWorld is null)
        {
            return await _feedbackService.SendContextualErrorAsync
            (
                $"You haven't set a default server! Please do so using the { Formatter.InlineQuote("/default-server") } command.",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        server = (ValidWorldDefinition)settings.DefaultWorld;

        return await SendWorldPopulationAsync(server).ConfigureAwait(false);
    }

    [Command("status")]
    [Description("Gets the status of a PlanetSide server.")]
    public async Task<IResult> GetServerStatusCommandAsync(
        [Description("Set your default server with '/default-server'.")] ValidWorldDefinition server = 0)
    {
        if (server == 0)
        {
            if (!_context.GuildID.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "To use this command in a DM you must provide a server.",
                    ct: CancellationToken).ConfigureAwait(false);
            }

            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            if (settings.DefaultWorld is null)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    $"You haven't set a default server! Please do so using the { Formatter.InlineQuote("/default-server") } command.",
                    ct: CancellationToken).ConfigureAwait(false);
            }

            server = (ValidWorldDefinition)settings.DefaultWorld;
        }

        return await SendWorldStatusAsync(server).ConfigureAwait(false);
    }

    private async Task<IResult> SendWorldPopulationAsync(ValidWorldDefinition world)
    {
        Result<IPopulation> populationResult = await _populationApi.GetWorldPopulationAsync(world, CancellationToken);
        Result<List<Map>> getMapsResult = await _censusApi.GetMapsAsync(world, ValidZones, CancellationToken);

        if (!populationResult.IsDefined(out IPopulation? population))
            return populationResult;

        List<string> unlockedZones = new();
        if (getMapsResult.IsDefined(out List<Map>? maps))
        {
            foreach (Map map in maps)
            {
                GetMapTerritoryControl(map, out double ncPercent, out double trPercent, out double vsPercent);
                if (ncPercent < 99 && trPercent < 99 && vsPercent < 99)
                    unlockedZones.Add(GetZoneName(map.ZoneID.Definition));
            }
        }

        List<EmbedField> embedFields = new()
        {
            new EmbedField("Unlocked Continents", string.Join(" ", unlockedZones)),
            new EmbedField($"{Formatter.Emoji("blue_circle")} NC - {population.NC}", BuildEmbedPopulationBar(population.NC, population.Total)),
            new EmbedField($"{Formatter.Emoji("red_circle")} TR - {population.TR}", BuildEmbedPopulationBar(population.TR, population.Total)),
            new EmbedField($"{Formatter.Emoji("purple_circle")} VS - {population.VS}", BuildEmbedPopulationBar(population.VS, population.Total)),
        };

        Embed embed = new()
        {
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Title = $"{world} - {population.Total}",
            Footer = new EmbedFooter("Pop data from Varunda's wt.honu.pw"),
            Fields = embedFields
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken).ConfigureAwait(false);
    }

    private async Task<IResult> SendWorldStatusAsync(ValidWorldDefinition world)
    {
        Result<List<Map>> getMapsResult = await _censusApi.GetMapsAsync(world, ValidZones, CancellationToken);

        if (!getMapsResult.IsDefined())
            return getMapsResult;

        List<EmbedField> embedFields = new();
        getMapsResult.Entity.Sort
        (
            (m1, m2) => string.CompareOrdinal(m1.ZoneID.Definition.ToString(), m2.ZoneID.Definition.ToString())
        );

        foreach (Map m in getMapsResult.Entity)
            embedFields.Add(GetMapStatusEmbedField(m, (WorldDefinition)world));

        Embed embed = new()
        {
            Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
            Title = world.ToString(),
            Fields = embedFields
        };

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken).ConfigureAwait(false);
    }

    private EmbedField GetMapStatusEmbedField(Map map, WorldDefinition world)
    {
        static string ConstructPopBar(double percent, string emojiName)
        {
            string result = string.Empty;
            for (int i = 0; i < Math.Round(percent / 10); i++)
                result += Formatter.Emoji(emojiName);

            return result;
        }

        GetMapTerritoryControl(map, out double ncPercent, out double trPercent, out double vsPercent);
        string title = GetZoneName(map.ZoneID.Definition);

        object cacheKey = CacheKeyHelpers.GetMetagameEventKey(world, map.ZoneID.Definition);
        if (_cache.TryGetValue(cacheKey, out IMetagameEvent? metagameEvent) && metagameEvent!.MetagameEventState is MetagameEventState.Started)
        {
            TimeSpan currentEventDuration = DateTimeOffset.UtcNow - metagameEvent.Timestamp;
            TimeSpan remainingTime = MetagameEventDefinitionToDuration.GetDuration(metagameEvent.MetagameEventID) - currentEventDuration;
            title += $" {Formatter.Emoji("rotating_light")} {remainingTime:%h\\h\\ %m\\m}";
        }
        else if (ncPercent > 99 || trPercent > 99 || vsPercent > 99)
        {
            title += " " + Formatter.Emoji("lock");
        }

        string popBar = ConstructPopBar(ncPercent, "blue_square");
        popBar += ConstructPopBar(trPercent, "red_square");
        popBar += ConstructPopBar(vsPercent, "purple_square");

        return new EmbedField(title, popBar);
    }

    private static string BuildEmbedPopulationBar(int population, int totalPopulation)
    {
        // Can't divide by zero!
        if (totalPopulation == 0 || population > totalPopulation)
            return Formatter.Emoji("black_large_square");

        double tensPercentage = Math.Ceiling((double)population / totalPopulation * 10);
        double remainder = 10 - tensPercentage;
        string result = string.Empty;

        for (int i = 0; i < tensPercentage; i++)
            result += Formatter.Emoji("green_square");

        for (int i = 0; i < remainder; i++)
            result += Formatter.Emoji("black_large_square");

        string stringPercentage = ((double)population / totalPopulation * 100).ToString("F1");
        result += $"{Formatter.Bold("   ")}({ stringPercentage }%)";

        return result;
    }

    private static void GetMapTerritoryControl
    (
        Map map,
        out double ncPercent,
        out double trPercent,
        out double vsPercent
    )
    {
        double regionCount = map.Regions.Row.Count(r => r.RowData.FactionID != FactionDefinition.None);
        ncPercent = (map.Regions.Row.Count(r => r.RowData.FactionID == FactionDefinition.NC) / regionCount) * 100;
        trPercent = (map.Regions.Row.Count(r => r.RowData.FactionID == FactionDefinition.TR) / regionCount) * 100;
        vsPercent = (map.Regions.Row.Count(r => r.RowData.FactionID == FactionDefinition.VS) / regionCount) * 100;
    }

    private static string GetZoneName(ZoneDefinition zone)
        => zone switch
        {
            ZoneDefinition.Amerish => $"{Formatter.Emoji("mountain")} {ZoneDefinition.Amerish}",
            ZoneDefinition.Esamir => $"{Formatter.Emoji("snowflake")} {ZoneDefinition.Esamir}",
            ZoneDefinition.Hossin => $"{Formatter.Emoji("deciduous_tree")} {ZoneDefinition.Hossin}",
            ZoneDefinition.Indar => $"{Formatter.Emoji("desert")} {ZoneDefinition.Indar}",
            ZoneDefinition.Koltyr => $"{Formatter.Emoji("radioactive")} {ZoneDefinition.Koltyr}",
            ZoneDefinition.Oshur => $"{Formatter.Emoji("ocean")} {ZoneDefinition.Oshur}",
            _ => zone.ToString()
        };
}
