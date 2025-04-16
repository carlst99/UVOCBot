using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Objects;
using Remora.Results;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Plugins.ApexLegends.Abstractions.Services;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Commands;

[Group("apex")]
[Description("Commands that facilitate the retrieval of Apex Legends game data")]
[Deferred]
public class ApexCommands : CommandGroup
{
    private readonly IApexApiService _apexApi;
    private readonly FeedbackService _feedbackService;

    public ApexCommands
    (
        IApexApiService apexApi,
        FeedbackService feedbackService
    )
    {
        _apexApi = apexApi;
        _feedbackService = feedbackService;
    }

    [Command("maps")]
    [Description("Gets the current map rotation.")]
    public async Task<Result> GetMapRotationsCommandAsync()
    {
        Result<IReadOnlyDictionary<string, MapRotations>> getRotations
            = await _apexApi.GetMapRotationsAsync(CancellationToken);

        if (!getRotations.IsDefined(out IReadOnlyDictionary<string, MapRotations>? rotations))
            return await NotifyOfApiRetrievalError((Result)getRotations).ConfigureAwait(false);

        List<Embed> embeds = [];
        if (rotations.TryGetValue("battle_royale", out MapRotations? brRotations))
            embeds.Add(BuildMapRotationEmbed("Battle Royal", brRotations));
        if (rotations.TryGetValue("ranked", out MapRotations? rankedRotations))
            embeds.Add(BuildMapRotationEmbed("Ranked", rankedRotations));

        if (embeds.Count > 0)
            return await _feedbackService.SendContextualEmbedsAsync(embeds, ct: CancellationToken);

        return await _feedbackService.SendContextualErrorAsync
        (
            "Map rotation data is currently unavailable",
            ct: CancellationToken
        );
    }

    private static Embed BuildMapRotationEmbed(string modeName, MapRotations rotations)
    {
        Embed embed = new
        (
            $"Current {modeName} map rotation - {rotations.Current?.Map ?? "Unknown"}",
            Description: $"Ends {Formatter.Timestamp(rotations.Current?.End ?? 0, TimestampStyle.RelativeTime)}",
            Colour: Color.Gold
        );

        if (rotations.Current?.Asset is not null)
            embed = embed with { Image = new EmbedImage(rotations.Current.Asset) };

        if (rotations.Next is not null)
        {
            embed = embed with
            {
                Fields = new EmbedField[]
                {
                    new($"Next - {rotations.Next.Map}", $"Will be open for {rotations.Next.DurationInSecs / 3600d} hours")
                }
            };
        }

        return embed;
    }

    private async Task<Result> NotifyOfApiRetrievalError(Result apiResult)
    {
        if (apiResult.Error is null)
            return Result.FromSuccess();

        if (apiResult.Error is ApexApiError)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                "The Apex Legends API is currently having troubles. Please try again later!",
                ct: CancellationToken
            ).ConfigureAwait(false);
        }

        return apiResult;
    }
}
