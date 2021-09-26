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
        private readonly IPermissionChecksService _permissionChecksService;

        public PlanetsideCommands(
            ICommandContext context,
            DiscordContext dbContext,
            IReplyService replyService,
            ICensusApiService censusApi,
            IPermissionChecksService permissionChecksService)
        {
            _context = context;
            _dbContext = dbContext;
            _replyService = replyService;
            _censusApi = censusApi;
            _permissionChecksService = permissionChecksService;
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
    }
}
