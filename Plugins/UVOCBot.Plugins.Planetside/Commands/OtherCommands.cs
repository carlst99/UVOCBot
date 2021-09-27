using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Core;
using Remora.Results;
using System.ComponentModel;
using System.Text;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Planetside.Objects.Census.Outfit;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Commands
{
    public enum TileMap
    {
        Amerish,
        Desolation,
        Esamir,
        Hossin,
        Indar,
        Koltyr,
        Sanctuary,
        Tutorial,
        VR
    }

    public enum NoDeploymentType
    {
        None,
        Sunderer,
        ANT
    }

    public class OtherCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly ICensusApiService _censusApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly FeedbackService _feedbackService;

        public OtherCommands(
            ICommandContext context,
            ICensusApiService censusApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestInteractionAPI interactionApi,
            FeedbackService feedbackService)
        {
            _context = context;
            _censusApi = censusApi;
            _channelApi = channelApi;
            _interactionApi = interactionApi;
            _feedbackService = feedbackService;
        }

        [Command("map")]
        [Description("Gets a PlanetSide 2 continent map.")]
        [Ephemeral]
        public async Task<IResult> MapCommandAsync(TileMap map, NoDeploymentType noDeployType = NoDeploymentType.None) // TODO: Complete
        {
            string mapFileName = map.ToString() + ".jpg";
            string mapFilePath = Path.Combine("Assets", "PS2Maps", mapFileName);

            DateTime mapLastUpdated = File.GetCreationTimeUtc(mapFilePath);

            Embed embed = new()
            {
                Title = map.ToString(),
                Author = new EmbedAuthor("Full-res maps here", "https://github.com/cooltrain7/Planetside-2-API-Tracker/tree/master/Maps"),
                Colour = DiscordConstants.DEFAULT_EMBED_COLOUR,
                Footer = new EmbedFooter("Map last updated " + mapLastUpdated.ToShortDateString()),
                Image = new EmbedImage("attachment://" + mapFileName),
                Type = EmbedType.Image,
            };

            if (_context is InteractionContext ictx)
            {
                return await _interactionApi.CreateFollowupMessageAsync(
                    ictx.ApplicationID,
                    ictx.Token,
                    file: new FileData(mapFileName, File.OpenRead(mapFilePath)),
                    embeds: new IEmbed[] { embed },
                    ct: CancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await _channelApi.CreateMessageAsync(
                    _context.ChannelID,
                    file: new FileData(mapFileName, File.OpenRead(mapFilePath)),
                    embeds: new IEmbed[] { embed },
                    ct: CancellationToken).ConfigureAwait(false);
            }
        }

        [Command("online")]
        [Description("Gets the number of online members for an outfit.")]
        [Ephemeral]
        public async Task<IResult> OnlineOutfitMembersCommandAsync(
            [Description("A space-separate, case-insensitive list of outfit tags.")] string outfitTags)
        {
            string[] tags = outfitTags.Split(' ');

            if (tags.Length == 0 || tags.Length > 10)
                return await _feedbackService.SendContextualErrorAsync("You must specify between 1-10 outfit tags.", ct: CancellationToken).ConfigureAwait(false);

            Result<List<OutfitOnlineMembers>> outfits = await _censusApi.GetOnlineMembersAsync(tags, CancellationToken).ConfigureAwait(false);
            if (!outfits.IsSuccess)
            {
                await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);
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
                    Colour = DiscordConstants.DEFAULT_EMBED_COLOUR
                });

                sb.Clear();
            }

            if (_context is InteractionContext ictx)
            {
                return await _interactionApi.CreateFollowupMessageAsync(
                    ictx.ApplicationID,
                    ictx.Token,
                    embeds: embeds,
                    ct: CancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await _channelApi.CreateMessageAsync(
                    _context.ChannelID,
                    embeds: embeds,
                    ct: CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
