using DbgCensus.Core.Objects;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Plugins.Planetside.Objects;
using UVOCBot.Plugins.Planetside.Objects.Census;
using UVOCBot.Plugins.Planetside.Objects.Census.Map;
using UVOCBot.Plugins.Planetside.Objects.Fisu;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Commands
{
    public class WorldCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly ICensusApiService _censusApi;
        private readonly IFisuApiService _fisuApi;
        private readonly DiscordContext _dbContext;
        private readonly FeedbackService _feedbackService;

        public WorldCommands(
            ICommandContext context,
            ICensusApiService censusApi,
            IFisuApiService fisuApi,
            DiscordContext dbContext,
            FeedbackService feedbackService)
        {
            _context = context;
            _censusApi = censusApi;
            _fisuApi = fisuApi;
            _dbContext = dbContext;
            _feedbackService = feedbackService;
        }

        [Command("default-server")]
        [Description("Sets the default world for planetside-related commands")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
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
        public async Task<IResult> PopulationCommandAsync(
            [Description("Set your default server with `/default-server`.")] ValidWorldDefinition server = 0)
        {
            if (server == 0)
            {
                if (!_context.GuildID.HasValue)
                {
                    return await _feedbackService.SendContextualErrorAsync(
                        "To use this command in a DM you must provide a server.", ct: CancellationToken).ConfigureAwait(false);
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
            Result<Population> populationResult = await _fisuApi.GetWorldPopulationAsync(world, CancellationToken).ConfigureAwait(false);

            if (!populationResult.IsSuccess)
            {
                await _feedbackService.SendContextualErrorAsync(
                    $"Could not get population statistics - the query to { Formatter.InlineQuote("fisu") } failed. Please try again.",
                    ct: CancellationToken).ConfigureAwait(false);

                return populationResult;
            }

            Population population = populationResult.Entity;

            Embed embed = new()
            {
                Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
                Title = world.ToString() + " - " + population.Total.ToString(),
                Footer = new EmbedFooter("Data gratefully taken from ps2.fisu.pw"),
                Fields = new List<EmbedField>
                {
                    new EmbedField($"{Formatter.Emoji("purple_circle")} VS - {population.VS}", BuildEmbedPopulationBar(population.VS, population.Total)),
                    new EmbedField($"{Formatter.Emoji("blue_circle")} NC - {population.NC}", BuildEmbedPopulationBar(population.NC, population.Total)),
                    new EmbedField($"{Formatter.Emoji("red_circle")} TR - {population.TR}", BuildEmbedPopulationBar(population.TR, population.Total)),
                    new EmbedField($"{Formatter.Emoji("white_circle")} NS - {population.NS}", BuildEmbedPopulationBar(population.NS, population.Total))
                }
            };

            return await _feedbackService.SendContextualEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
        }

        private async Task<IResult> SendWorldStatusAsync(ValidWorldDefinition world)
        {
            Task<Result<List<Map>>> getMapsTask = _censusApi.GetMapsAsync(world, Enum.GetValues<ValidZoneDefinition>(), CancellationToken);
            Task<Result<List<MetagameEvent>>> getMetagameEventsTask = _censusApi.GetMetagameEventsAsync(world, CancellationToken);

            Result<List<Map>> getMapsResult = await getMapsTask.ConfigureAwait(false);
            Result<List<MetagameEvent>> getMetagameEventsResult = await getMetagameEventsTask.ConfigureAwait(false);

            if (!getMapsResult.IsSuccess || !getMetagameEventsResult.IsSuccess)
                return await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);

            List<EmbedField> embedFields = new();
            foreach (Map m in getMapsResult.Entity)
                embedFields.Add(GetMapStatusEmbedField(m, getMetagameEventsResult.Entity));

            Embed embed = new()
            {
                Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
                Description = BuildWorldStatusString(getMetagameEventsResult.Entity[0].World),
                Title = world.ToString(),
                Fields = embedFields
            };

            return await _feedbackService.SendContextualEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
        }

        private static EmbedField GetMapStatusEmbedField(Map map, List<MetagameEvent> metagameEvents)
        {
            static string ConstructPopBar(double percent, string emojiName)
            {
                string result = string.Empty;
                for (int i = 0; i < Math.Round(percent / 10); i++)
                    result += Formatter.Emoji(emojiName);

                return result;
            }

            double regionCount = map.Regions.Row.Count(r => r.RowData.FactionId != Faction.None);
            double ncPercent = (map.Regions.Row.Count(r => r.RowData.FactionId == Faction.NC) / regionCount) * 100;
            double trPercent = (map.Regions.Row.Count(r => r.RowData.FactionId == Faction.TR) / regionCount) * 100;
            double vsPercent = (map.Regions.Row.Count(r => r.RowData.FactionId == Faction.VS) / regionCount) * 100;

            string title = map.ZoneId.Definition switch
            {
                ZoneDefinition.Amerish => Formatter.Emoji("mountain") + " Amerish",
                ZoneDefinition.Esamir => Formatter.Emoji("snowflake") + " Esamir",
                ZoneDefinition.Hossin => Formatter.Emoji("deciduous_tree") + " Hossin",
                ZoneDefinition.Indar => Formatter.Emoji("desert") + " Indar",
                ZoneDefinition.Koltyr => Formatter.Emoji("zap") + " Koltyr",
                _ => map.ZoneId.Definition.ToString()
            };

            if (Math.Round(ncPercent, 0) == 100 || Math.Round(trPercent, 0) == 100 || Math.Round(vsPercent, 0) == 100)
                title += " " + Formatter.Emoji("lock");

            if (metagameEvents.Find(m => m.ZoneId == map.ZoneId)?.MetagameEventStateName == "started")
                title += " " + Formatter.Emoji("rotating_light");

            string popBar = ConstructPopBar(ncPercent, "blue_square");
            popBar += ConstructPopBar(trPercent, "red_square");
            popBar += ConstructPopBar(vsPercent, "purple_square");

            return new EmbedField(title, popBar);
        }

        private static string BuildWorldStatusString(World world)
            => world.State switch
            {
                "online" => $"Status: Online {Formatter.Emoji("green_circle")}",
                "offline" => $"Status: Offline {Formatter.Emoji("red_circle")}",
                "locked" => $"Status: Locked {Formatter.Emoji("red_circle")}",
                _ => $"Status: Unknown {Formatter.Emoji("black_circle")}"
            };

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
    }
}
