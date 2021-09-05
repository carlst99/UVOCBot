using DbgCensus.Core.Objects;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Commands.Conditions.Attributes;
using UVOCBot.Core.Dto;
using UVOCBot.Model;
using UVOCBot.Model.Census;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    public class PlanetsideCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly IReplyService _replyService;
        private readonly IDbApiService _dbApi;
        private readonly ICensusApiService _censusApi;
        private readonly IFisuApiService _fisuAPI;

        public PlanetsideCommands(ICommandContext context, IReplyService responder, IDbApiService dbApi, ICensusApiService censusApi, IFisuApiService fisuAPI)
        {
            _context = context;
            _replyService = responder;
            _dbApi = dbApi;
            _censusApi = censusApi;
            _fisuAPI = fisuAPI;
        }

        [Command("population")]
        [Description("Gets the population of a PlanetSide server.")]
        public async Task<IResult> GetServerPopulationCommandAsync(
            [Description("Set your default server with `/default-server`.")] WorldType server = 0)
        {
            if (server == 0)
            {
                if (!_context.GuildID.HasValue)
                    return await _replyService.RespondWithUserErrorAsync("To use this command in a DM you must provide a server.", CancellationToken).ConfigureAwait(false);

                Result<PlanetsideSettingsDto> settings = await _dbApi.GetPlanetsideSettingsAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
                if (!settings.IsSuccess)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return settings;
                }

                if (settings.Entity.DefaultWorld is null)
                {
                    return await _replyService.RespondWithUserErrorAsync(
                        $"You haven't set a default server! Please do so using the {Formatter.InlineQuote("/default-server")} command.",
                        CancellationToken).ConfigureAwait(false);
                }

                server = (WorldType)settings.Entity.DefaultWorld;
            }

            return await SendWorldPopulation(server).ConfigureAwait(false);
        }

        [Command("status")]
        [Description("Gets the status of a PlanetSide server.")]
        public async Task<IResult> GetServerStatusCommandAsync(
            [Description("Set your default server with '/default-server'.")] WorldType server = 0)
        {
            if (server == 0)
            {
                if (!_context.GuildID.HasValue)
                    return await _replyService.RespondWithUserErrorAsync("To use this command in a DM you must provide a server.", CancellationToken).ConfigureAwait(false);

                Result<PlanetsideSettingsDto> settings = await _dbApi.GetPlanetsideSettingsAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
                if (!settings.IsSuccess)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return settings;
                }

                if (settings.Entity.DefaultWorld is null)
                {
                    return await _replyService.RespondWithUserErrorAsync(
                        $"You haven't set a default server! Please do so using the {Formatter.InlineQuote("/default-server")} command.",
                        CancellationToken).ConfigureAwait(false);
                }

                server = (WorldType)settings.Entity.DefaultWorld;
            }

            return await SendWorldStatus(server).ConfigureAwait(false);
        }

        [Command("online")]
        [Description("Gets the number of online members for an outfit.")]
        [Ephemeral]
        public async Task<IResult> GetOnlineOutfitMembersCommandAsync([Description("A space-separated, case-insensitive list of outfit tags.")] string outfitTags)
        {
            string[] tags = outfitTags.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (tags.Length == 1)
            {
                Result<OutfitOnlineMembers> onlineMembers = await _censusApi.GetOnlineMembersAsync(tags[0], CancellationToken).ConfigureAwait(false);
                if (!onlineMembers.IsSuccess)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return onlineMembers;
                }

                StringBuilder sb = new();
                foreach (OutfitOnlineMembers.MemberModel member in onlineMembers.Entity.OnlineMembers.OrderBy(m => m.Character.Name.First))
                    sb.AppendLine(member.Character.Name.First);

                Embed embed = new()
                {
                    Author = new EmbedAuthor($"{ onlineMembers.Entity.OutfitAlias } | { onlineMembers.Entity.OutfitName }"),
                    Title = onlineMembers.Entity.OnlineMembers.Count.ToString() + " online",
                    Description = sb.ToString(),
                    Colour = BotConstants.DEFAULT_EMBED_COLOUR
                };

                return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
            }
            else if (tags.Length > 1)
            {
                Result<List<OutfitOnlineMembers>> outfits = await _censusApi.GetOnlineMembersAsync(tags, CancellationToken).ConfigureAwait(false);
                if (!outfits.IsSuccess)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return outfits;
                }

                List<IEmbedField> embedFields = new();
                foreach (OutfitOnlineMembers onlineMembers in outfits.Entity)
                {
                    embedFields.Add(new EmbedField
                        (
                            onlineMembers.OutfitAlias + " | " + onlineMembers.OutfitName,
                            onlineMembers.OnlineMembers.Count.ToString() + " online"
                        ));
                }

                Embed embed = new()
                {
                    Fields = embedFields,
                    Colour = BotConstants.DEFAULT_EMBED_COLOUR
                };

                return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
            }

            return Result.FromSuccess();
        }

        [Command("map")]
        [Description("Gets a PlanetSide 2 continent map.")]
        public async Task<IResult> MapCommand(TileMap map, NoDeploymentType noDeployType = NoDeploymentType.None)
        {
            string mapFileName = map.ToString() + ".jpg";
            string mapFilePath = Path.Combine("Assets", "PS2Maps", mapFileName);

            DateTime mapLastUpdated = File.GetCreationTimeUtc(mapFilePath);

            Embed embed = new()
            {
                Title = map.ToString(),
                Author = new EmbedAuthor("Full-res maps here", "https://github.com/cooltrain7/Planetside-2-API-Tracker/tree/master/Maps"),
                Colour = BotConstants.DEFAULT_EMBED_COLOUR,
                Footer = new EmbedFooter("Map last updated " + mapLastUpdated.ToShortDateString()),
                Image = new EmbedImage("attachment://" + mapFileName),
                Type = EmbedType.Image,
            };

            return await _replyService.RespondWithEmbedAsync(embed, CancellationToken, new FileData(mapFileName, File.OpenRead(mapFilePath))).ConfigureAwait(false);
        }

        [Command("default-server")]
        [Description("Sets the default world for planetside-related commands")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> DefaultWorldCommand([DiscordTypeHint(TypeHint.String)] WorldType server)
        {
            Result<PlanetsideSettingsDto> settings = await _dbApi.GetPlanetsideSettingsAsync(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            if (!settings.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return settings;
            }

            settings.Entity.DefaultWorld = (int)server;

            Result updateResult = await _dbApi.UpdatePlanetsideSettingsAsync(_context.GuildID.Value.Value, settings.Entity, CancellationToken).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return updateResult;
            }

            return await _replyService.RespondWithSuccessAsync(
                $"{Formatter.Emoji("earth_asia")} Your default server has been set to {Formatter.InlineQuote(server.ToString())}",
                ct: CancellationToken).ConfigureAwait(false);
        }

        private async Task<IResult> SendWorldPopulation(WorldType world)
        {
            Result<FisuPopulation> populationResult = await _fisuAPI.GetContinentPopulationAsync(world, CancellationToken).ConfigureAwait(false);

            if (!populationResult.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(
                    CancellationToken,
                    $"Could not get population statistics - the query to { Formatter.InlineQuote("fisu") } failed. Please try again.").ConfigureAwait(false);
                return populationResult;
            }

            FisuPopulation population = populationResult.Entity;

            Embed embed = new()
            {
                Colour = BotConstants.DEFAULT_EMBED_COLOUR,
                Title = world.ToString() + " - " + population.Total.ToString(),
                Footer = new EmbedFooter("Data gratefully taken from ps2.fisu.pw"),
                Fields = new List<EmbedField>
                {
                    new EmbedField($"{Formatter.Emoji("purple_circle")} VS - {population.VS}", GetEmbedPopulationBar(population.VS, population.Total)),
                    new EmbedField($"{Formatter.Emoji("blue_circle")} NC - {population.NC}", GetEmbedPopulationBar(population.NC, population.Total)),
                    new EmbedField($"{Formatter.Emoji("red_circle")} TR - {population.TR}", GetEmbedPopulationBar(population.TR, population.Total)),
                    new EmbedField($"{Formatter.Emoji("white_circle")} NS - {population.NS}", GetEmbedPopulationBar(population.NS, population.Total))
                }
            };

            return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
        }

        private async Task<IResult> SendWorldStatus(WorldType world)
        {
            try
            {
                List<Map> maps = await _censusApi.GetMaps(world, Enum.GetValues<ZoneDefinition>(), CancellationToken).ConfigureAwait(false);
                List<MetagameEvent> events = await _censusApi.GetMetagameEventsAsync(world, CancellationToken).ConfigureAwait(false);

                List<EmbedField> embedFields = new();
                foreach (Map m in maps)
                    embedFields.Add(GetMapStatusEmbedField(m, events));

                Embed embed = new()
                {
                    Colour = BotConstants.DEFAULT_EMBED_COLOUR,
                    Description = GetWorldStatusString(events[0].World),
                    Title = world.ToString(),
                    Fields = embedFields
                };

                return await _replyService.RespondWithEmbedAsync(embed, CancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
            }
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

            // TODO: This is completely unreliable. Could be a map endpoint issue
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
                _ => Formatter.Emoji("no_entry_sign") + " Unknown Continent"
            };

            // Can at least fix this easily by printing the result of the metagame event.
            if (Math.Round(ncPercent, 0) == 100 || Math.Round(trPercent, 0) == 100 || Math.Round(vsPercent, 0) == 100)
                title += " " + Formatter.Emoji("lock");

            if (metagameEvents.Find(m => m.ZoneId == map.ZoneId)?.MetagameEventStateName == "started")
                title += " " + Formatter.Emoji("rotating_light");

            string popBar = ConstructPopBar(ncPercent, "blue_square");
            popBar += ConstructPopBar(trPercent, "red_square");
            popBar += ConstructPopBar(vsPercent, "purple_square");

            return new EmbedField(title, popBar);
        }

        private static string GetWorldStatusString(World world)
            => world.State switch
            {
                "online" => $"Status: Online {Formatter.Emoji("green_circle")}",
                "offline" => $"Status: Offline {Formatter.Emoji("red_circle")}",
                "locked" => $"Status: Locked {Formatter.Emoji("red_circle")}",
                _ => $"Status: Unknown {Formatter.Emoji("black_circle")}"
            };

        private static string GetEmbedPopulationBar(int population, int totalPopulation)
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
