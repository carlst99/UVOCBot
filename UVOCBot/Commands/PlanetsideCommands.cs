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
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Model;
using UVOCBot.Model.Census;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    public class PlanetsideCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IReplyService _replyService;
        private readonly ICensusApiService _censusApi;
        private readonly IFisuApiService _fisuAPI;
        private readonly IPermissionChecksService _permissionChecksService;

        public PlanetsideCommands(
            ICommandContext context,
            DiscordContext dbContext,
            IReplyService replyService,
            ICensusApiService censusApi,
            IFisuApiService fisuAPI,
            IPermissionChecksService permissionChecksService)
        {
            _context = context;
            _dbContext = dbContext;
            _replyService = replyService;
            _censusApi = censusApi;
            _fisuAPI = fisuAPI;
            _permissionChecksService = permissionChecksService;
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

                PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

                if (settings.DefaultWorld is null)
                {
                    return await _replyService.RespondWithUserErrorAsync(
                        $"You haven't set a default server! Please do so using the {Formatter.InlineQuote("/default-server")} command.",
                        CancellationToken).ConfigureAwait(false);
                }

                server = (WorldType)settings.DefaultWorld;
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

                PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

                if (settings.DefaultWorld is null)
                {
                    return await _replyService.RespondWithUserErrorAsync(
                        $"You haven't set a default server! Please do so using the {Formatter.InlineQuote("/default-server")} command.",
                        CancellationToken).ConfigureAwait(false);
                }

                server = (WorldType)settings.DefaultWorld;
            }

            return await SendWorldStatus(server).ConfigureAwait(false);
        }

        [Command("online")]
        [Description("Gets the number of online members for an outfit.")]
        [Ephemeral]
        public async Task<IResult> GetOnlineOutfitMembersCommandAsync(
            [Description("A space-separate, case-insensitive list of outfit tags.")] string outfitTags)
        {
            string[] tags = outfitTags.Split(' ');

            if (tags.Length == 0 || tags.Length > 10)
                return await _replyService.RespondWithUserErrorAsync("You must specify between 1-10 outfit tags.", CancellationToken).ConfigureAwait(false);

            Result<List<OutfitOnlineMembers>> outfits = await _censusApi.GetOnlineMembersAsync(tags, CancellationToken).ConfigureAwait(false);
            if (!outfits.IsSuccess)
            {
                await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                return outfits;
            }

            StringBuilder sb = new();
            List<Embed> embeds = new();

            foreach (OutfitOnlineMembers oufit in outfits.Entity)
            {
                foreach (OutfitOnlineMembers.MemberModel member in oufit.OnlineMembers.OrderBy(m => m.Character.Name.First))
                    sb.AppendLine(member.Character.Name.First);

                embeds.Add(new()
                {
                    Title = $"[{ oufit.OutfitAlias }] { oufit.OutfitName } - { oufit.OnlineMembers.Count } online.",
                    Description = oufit.OnlineMembers.Count > 0 ? sb.ToString() : @"¯\_(ツ)_/¯",
                    Colour = BotConstants.DEFAULT_EMBED_COLOUR
                });

                sb.Clear();
            }

            return await _replyService.RespondWithEmbedAsync(embeds, CancellationToken).ConfigureAwait(false);
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
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            settings.DefaultWorld = (int)server;

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync(
                $"{Formatter.Emoji("earth_asia")} Your default server has been set to {Formatter.InlineQuote(server.ToString())}",
                ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("track-outfit")]
        [Description("Tracks an outfit's base captures.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> TrackOutfitCommandAsync(string outfitTag)
        {
            Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(outfitTag, CancellationToken).ConfigureAwait(false);
            if (!getOutfitResult.IsSuccess)
                return await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
            if (getOutfitResult.Entity is null)
                return await _replyService.RespondWithUserErrorAsync("That outfit doesn't exist.", CancellationToken).ConfigureAwait(false);

            Outfit outfit = getOutfitResult.Entity;
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            if (!settings.TrackedOutfits.Contains(outfit.OutfitId))
                settings.TrackedOutfits.Add(outfit.OutfitId);

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync($"Now tracking [{ outfit.Alias }] { outfit.Name }", CancellationToken).ConfigureAwait(false);
        }

        [Command("untrack-outfit")]
        [Description("Removes a tracked outfit.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> UntrackOutfitCommandAsync(
            [Description("The 1-4 letter tag of the outfit to track.")] string outfitTag)
        {
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);

            Result<Outfit?> getOutfitResult = await _censusApi.GetOutfitAsync(outfitTag, CancellationToken).ConfigureAwait(false);
            if (!getOutfitResult.IsSuccess)
                return await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);

            /* We're not worrying about the outfit not existing here, as it isn't a huge concern to leave them sitting around
             * if the in-game outfit has been deleted.
             */
            if (getOutfitResult.Entity is not null && settings.TrackedOutfits.Contains(getOutfitResult.Entity.OutfitId))
                settings.TrackedOutfits.Remove(getOutfitResult.Entity.OutfitId);

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync("That outfit is no longer tracked.", CancellationToken).ConfigureAwait(false);
        }

        [Command("base-capture-channel")]
        [Description("Sets the channel to post base capture notifications in for any tracked outfits.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireGuildPermission(DiscordPermission.ManageGuild, false)]
        public async Task<IResult> SetBaseCaptureChannelCommandAsync(
            [Description("The channel. Leave empty to disable base capture notifications.")] IChannel? channel = null)
        {
            PlanetsideSettings settings = await _dbContext.FindOrDefaultAsync<PlanetsideSettings>(_context.GuildID.Value.Value, CancellationToken).ConfigureAwait(false);
            settings.BaseCaptureChannelId = null;

            if (channel is not null)
            {
                if (channel.Type != ChannelType.GuildText)
                    return await _replyService.RespondWithUserErrorAsync(Formatter.ChannelMention(channel) + " must be a text channel.", CancellationToken).ConfigureAwait(false);

                Result<IDiscordPermissionSet> getPermissionSet = await _permissionChecksService.GetPermissionsInChannel(channel, BotConstants.UserId, CancellationToken).ConfigureAwait(false);
                if (!getPermissionSet.IsSuccess)
                {
                    await _replyService.RespondWithErrorAsync(CancellationToken).ConfigureAwait(false);
                    return getPermissionSet;
                }

                if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.ViewChannel))
                    return await _replyService.RespondWithUserErrorAsync("I do not have permission to view " + Formatter.ChannelMention(channel), CancellationToken).ConfigureAwait(false);

                if (!getPermissionSet.Entity.HasAdminOrPermission(DiscordPermission.SendMessages))
                    return await _replyService.RespondWithUserErrorAsync("I do not have permission to send messages in " + Formatter.ChannelMention(channel), CancellationToken).ConfigureAwait(false);

                settings.BaseCaptureChannelId = channel.ID.Value;
            }

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            string message = channel is null ? "Base capture notifications have been disabled" : "Base capture notifications will now be sent to " + Formatter.ChannelMention(channel);
            return await _replyService.RespondWithSuccessAsync(message, CancellationToken).ConfigureAwait(false);
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
