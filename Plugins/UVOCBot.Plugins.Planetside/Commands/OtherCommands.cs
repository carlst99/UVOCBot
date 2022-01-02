using OneOf;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.Commands;

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
            return await _interactionApi.CreateFollowupMessageAsync
            (
                ictx.ApplicationID,
                ictx.Token,
                embeds: new IEmbed[] { embed },
                attachments: new OneOf<FileData, IPartialAttachment>[] { new FileData(mapFileName, File.OpenRead(mapFilePath)) },
                ct: CancellationToken
            ).ConfigureAwait(false);
        }
        else
        {
            return await _channelApi.CreateMessageAsync(
                _context.ChannelID,
                embeds: new IEmbed[] { embed },
                attachments: new[] { OneOf<FileData, IPartialAttachment>.FromT0(new FileData(mapFileName, File.OpenRead(mapFilePath))) },
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
        if (!outfits.IsDefined())
        {
            await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);
            return outfits;
        }

        // TODO: Fails for outfits that don't exist

        StringBuilder sb = new();
        List<Embed> embeds = new();

        foreach (OutfitOnlineMembers outfit in outfits.Entity)
        {
            int onlineMemberCount = 0;

            if (outfit.OnlineMembers is null || outfit.OnlineMembers.Count == 0)
            {
                sb.Append(@"¯\_(ツ)_/¯");
            }
            else
            {
                foreach (OutfitOnlineMembers.MemberModel member in outfit.OnlineMembers.OrderBy(m => m.Character.Name.First))
                    sb.AppendLine(member.Character.Name.First);

                onlineMemberCount = outfit.OnlineMembers.Count;
            }

            embeds.Add(new()
            {
                Title = $"[{ outfit.OutfitAlias }] { outfit.OutfitName } - { onlineMemberCount } online.",
                Description = sb.ToString(),
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
