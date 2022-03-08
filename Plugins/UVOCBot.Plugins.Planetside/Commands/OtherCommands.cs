using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.Commands;

public class OtherCommands : CommandGroup
{
    private readonly ICensusApiService _censusApi;
    private readonly FeedbackService _feedbackService;

    public OtherCommands
    (
        ICensusApiService censusApi,
        FeedbackService feedbackService
    )
    {
        _censusApi = censusApi;
        _feedbackService = feedbackService;
    }

    [Command("online")]
    [Description("Gets the number of online members for an outfit.")]
    [Ephemeral]
    [Deferred]
    public async Task<Result> OnlineOutfitMembersCommandAsync
    (
        [Description("A space-separate, case-insensitive list of outfit tags.")] string outfitTags
    )
    {
        string[] tags = outfitTags.Split(' ');

        if (tags.Length is 0 or > 10)
            return await _feedbackService.SendContextualErrorAsync("You must specify between 1-10 outfit tags.", ct: CancellationToken).ConfigureAwait(false);

        Result<List<OutfitOnlineMembers>> outfits = await _censusApi.GetOnlineMembersAsync(tags, CancellationToken).ConfigureAwait(false);
        if (!outfits.IsDefined())
        {
            await _feedbackService.SendContextualErrorAsync(DiscordConstants.GENERIC_ERROR_MESSAGE, ct: CancellationToken).ConfigureAwait(false);
            return Result.FromError(outfits);
        }

        if (outfits.Entity.Count == 0)
            return await _feedbackService.SendContextualWarningAsync("None of the provided outfit tags exist", ct: CancellationToken);

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

            embeds.Add(new Embed
            {
                Title = $"[{ outfit.OutfitAlias }] { outfit.OutfitName } - { onlineMemberCount } online.",
                Description = sb.ToString(),
                Colour = DiscordConstants.DEFAULT_EMBED_COLOUR
            });

            sb.Clear();
        }

        return await _feedbackService.SendContextualEmbedsAsync(embeds, ct: CancellationToken);
    }
}
