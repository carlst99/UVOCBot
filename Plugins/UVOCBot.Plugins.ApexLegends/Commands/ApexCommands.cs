using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
    private readonly IApexImageGenerationService _imageGenerationService;
    private readonly FeedbackService _feedbackService;

    public ApexCommands
    (
        IApexApiService apexApi,
        IApexImageGenerationService imageGenerationService,
        FeedbackService feedbackService
    )
    {
        _apexApi = apexApi;
        _imageGenerationService = imageGenerationService;
        _feedbackService = feedbackService;
    }

    [Command("maps")]
    [Description("Gets the current map rotation.")]
    public async Task<Result> GetMapRotationsCommandAsync()
    {
        Result<MapRotationBundle> getRotations = await _apexApi.GetMapRotationsAsync(CancellationToken)
            .ConfigureAwait(false);

        if (!getRotations.IsDefined(out MapRotationBundle? rotations))
            return await NotifyOfApiRetrievalError((Result)getRotations).ConfigureAwait(false);

        Embed embed = new
        (
            $"Current map rotation - {rotations.Current.Map}",
            Description: $"Ends {Formatter.Timestamp(rotations.Current.End, TimestampStyle.RelativeTime)}",
            Colour: Color.Gold
        );

        if (rotations.Current.Asset is not null)
            embed = embed with { Image = new EmbedImage(rotations.Current.Asset) };

        if (rotations.Next is not null)
        {
            embed = embed with
            {
                Fields = new EmbedField[]
                {
                    new($"Next - {rotations.Next.Map}", $"Will be open for {rotations.Next.DurationInSecs / 60} minutes")
                }
            };
        }

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken).ConfigureAwait(false);
    }

    [Command("craftables")]
    public async Task<Result> GetCraftingInformationCommandAsync()
    {
        Result<List<CraftingBundle>> getBundles = await _apexApi.GetCraftingBundlesAsync(CancellationToken)
            .ConfigureAwait(false);

        if (!getBundles.IsSuccess)
            return await NotifyOfApiRetrievalError((Result)getBundles).ConfigureAwait(false);

        List<EmbedField> fields = new();
        long dailyRotationEnd = 0;

        List<CraftingBundle> bundles = getBundles.Entity
            .Where(x => x.BundleType is CraftingBundleType.Daily or CraftingBundleType.Weekly)
            .OrderBy(x => x.BundleType)
            .ToList();

        foreach (CraftingBundle bundle in bundles)
        {
            if (DateTimeOffset.FromUnixTimeSeconds(bundle.Start) >= DateTimeOffset.UtcNow)
                continue;

            if (bundle.BundleType is CraftingBundleType.Daily)
                dailyRotationEnd = bundle.End;

            fields.Add(CreateBundleEmbedField(bundle));
        }

        using MemoryStream imageStream = await _imageGenerationService.GenerateCraftingBundleImageAsync(bundles, CancellationToken)
            .ConfigureAwait(false);

        Embed embed = new
        (
            "Current Crafting Bundles",
            Description: $"Daily rotations change {Formatter.Timestamp(dailyRotationEnd, TimestampStyle.RelativeTime)}",
            Colour: Color.Gold,
            Image: new EmbedImage("attachment://craftables.png"),
            Fields: fields
        );

        return await _feedbackService.SendContextualEmbedAsync
        (
            embed,
            new FeedbackMessageOptions
            (
                Attachments: new OneOf.OneOf<FileData, IPartialAttachment>[]
                {
                    new FileData("craftables.png", imageStream, "A visualisation of the current crafting bundles")
                }
            ),
            ct: CancellationToken
        ).ConfigureAwait(false);
    }

    [Command("player")]
    public async Task<Result> GetPlayerStatisticsCommandAsync(string playerName, PlayerPlatform platform = PlayerPlatform.Origin)
    {
        Result<StatsBridge> getStats = await _apexApi.GetPlayerStatisticsAsync(playerName, platform, CancellationToken)
            .ConfigureAwait(false);

        if (!getStats.IsDefined(out StatsBridge? stats))
        {
            if (getStats.Error is ApexApiError apexError && apexError.Message.Contains("not found"))
            {
                return await _feedbackService.SendContextualWarningAsync
                (
                    "That player could not be found. Ensure you are using their Origin name.",
                    ct: CancellationToken
                ).ConfigureAwait(false);
            }

            return await NotifyOfApiRetrievalError((Result)getStats).ConfigureAwait(false);
        }

        List<EmbedField> fields = new();

        if (stats.Global.Bans.IsActive)
        {
            fields.Add(new EmbedField
            (
                $"Banned for {TimeSpan.FromSeconds(stats.Global.Bans.RemainingSeconds):hh\\h\\ mm\\m}",
                $"Reason: {stats.Global.Bans.LastBanReason}"
            ));
        }

        fields.Add(new EmbedField
        (
            "Status",
            CreateStatusText(stats.Realtime),
            true
        ));

        fields.Add(new EmbedField
        (
            $"Level {stats.Global.Level}",
            $"Prestige count: {stats.Global.LevelPrestige}\n"
                + $"Percent to next level: {stats.Global.ToNextLevelPercent}"
        ));

        StatsBridgeGlobal.RankInfo rank = stats.Global.Rank;
        fields.Add(new EmbedField
        (
            $"Ranked: {rank.RankName} {RankDivisionToString(rank.RankDiv)}",
            $"Score: {rank.RankScore}",
            true
        ));

        StatsBridgeGlobal.RankInfo arenaRank = stats.Global.Arena;
        fields.Add(new EmbedField
        (
            $"Arenas Ranked: {arenaRank.RankName} {RankDivisionToString(arenaRank.RankDiv)}",
            $"Score: {arenaRank.RankScore}",
            true
        ));

        string statusEmoji = stats.Realtime.IsOnline == 1
            ? Formatter.Emoji("green_circle")
            : Formatter.Emoji("red_circle");

        Embed embed = new
        (
            $"Statistics for {stats.Global.Name} ({stats.Global.Platform}) {statusEmoji}",
            Colour: Color.Gold,
            Thumbnail: new EmbedThumbnail(stats.Global.Rank.RankImg),
            Image: new EmbedImage(stats.Legends.Selected.ImgAssets.Banner),
            Fields: fields
        );

        return await _feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken)
            .ConfigureAwait(false);
    }

    private static EmbedField CreateBundleEmbedField(CraftingBundle bundle)
    {
        StringBuilder sb = new();

        foreach (CraftingBundleContent content in bundle.BundleContent.OrderBy(x => x.ItemType.Rarity))
        {
            sb.Append(content.ItemType.Rarity)
                .Append(": ")
                .Append(SnakeCaseToHumanReadable(content.ItemType.Name))
                .Append(" | ")
                .AppendLine(content.Cost.ToString());
        }

        return new EmbedField
        (
            $"{bundle.BundleType} - {SnakeCaseToHumanReadable(bundle.Bundle)}",
            sb.ToString()
        );
    }

    private static string SnakeCaseToHumanReadable(string name)
    {
        ReadOnlySpan<char> fromBuf = name;
        Span<char> toBuf = stackalloc char[name.Length];
        bool convertToCapital = true;

        for (int i = 0; i < name.Length; i++)
        {
            char element = fromBuf[i];

            if (element is '_')
            {
                convertToCapital = true;
                toBuf[i] = ' ';
                continue;
            }

            if (convertToCapital)
            {
                element = char.ToUpper(element);
                convertToCapital = false;
            }

            toBuf[i] = element;
        }

        return new string(toBuf);
    }

    private static string CreateStatusText(StatsBridgeRealtime realtimeData)
    {
        if (realtimeData.IsOnline == 0)
            return "Offline";

        string result = realtimeData.IsInGame == 0
            ? "In lobby"
            : "In match";

        if (realtimeData.CurrentStateSecsAgo is { } stateForSecs)
            result += $" ({TimeSpan.FromSeconds(stateForSecs):mm\\:ss})";

        if (realtimeData.CanJoin == 0)
            result += " (invite only)";

        if (realtimeData.PartyFull > 0)
            result += " (party full)";

        if (realtimeData.IsOnline > 0)
            result += $"\nCurrent legend: {realtimeData.SelectedLegend}";

        return result;
    }

    private static string RankDivisionToString(int division)
        => division switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => string.Empty
        };

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
