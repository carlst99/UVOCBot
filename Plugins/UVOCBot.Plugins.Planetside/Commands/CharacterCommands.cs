using DbgCensus.Core.Objects;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using DbgCensus.Rest.Objects;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands;
using UVOCBot.Discord.Core.Commands.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;

namespace UVOCBot.Plugins.Planetside.Commands;

public class CharacterCommands : CommandGroup
{
    private readonly IQueryService _queryService;
    private readonly FeedbackService _feedbackService;

    public CharacterCommands(IQueryService queryService, FeedbackService feedbackService)
    {
        _queryService = queryService;
        _feedbackService = feedbackService;
    }

    #pragma warning disable CS8509
    [Command("character")]
    [Description("Gets basic info about a PlanetSide character.")]
    [Deferred]
    public async Task<Result> GetCharacterInfoCommandAsync(string characterName)
    {
        IQueryBuilder query = _queryService.CreateQuery()
            .OnCollection("character")
            .WithLanguage(CensusLanguage.English)
            .Where("name.first_lower", SearchModifier.Equals, characterName.ToLower())
            .HideFields("certs", "daily_ribbon", "head_id", "profile_id")
            .AddResolve("online_status")
            .AddResolve("world");

        query.AddJoin("experience_rank")
            .OnField("battle_rank.value")
            .ToField("rank")
            .Where("vs.title.en", SearchModifier.NotEquals, "A.S.P. Operative")
            .InjectAt("icons")
            .ShowFields("vs_image_path", "nc_image_path", "tr_image_path");

        query.AddJoin("title")
            .InjectAt("title_info");

        query.AddJoin("characters_stat_history")
            .Where("stat_name", SearchModifier.Equals, "kills")
            .ShowFields("all_time", "month.m01")
            .InjectAt("kills");

        query.AddJoin("characters_stat_history")
            .Where("stat_name", SearchModifier.Equals, "deaths")
            .ShowFields("all_time", "month.m01")
            .InjectAt("deaths");

        Result<CharacterInfo?> characterResult = await _queryService.GetAsync<CharacterInfo>(query, CancellationToken);
        if (!characterResult.IsSuccess)
            return Result.FromError(characterResult);

        if (!characterResult.IsDefined(out CharacterInfo? character))
            return new GenericCommandError("That character doesn't exist");

        string title = character.OnlineStatus
            ? Formatter.Emoji("green_circle")
            : Formatter.Emoji("red_circle");

        title += " ";
        title += character.TitleInfo is null
            ? character.Name.First
            : character.TitleInfo.Name.English + " " + character.Name.First;

        string description = $"Of {character.WorldID}'s {character.FactionID}";

        string? iconUrl = "https://census.daybreakgames.com" + character.PrestigeLevel switch
        {
            0 when character.FactionID is FactionDefinition.NC => character.BattleRank.Icons.NCImagePath,
            0 when character.FactionID is FactionDefinition.TR => character.BattleRank.Icons.TRImagePath,
            0 when character.FactionID is FactionDefinition.VS => character.BattleRank.Icons.VSImagePath,
            0 => string.Empty,
            1 => "/files/ps2/images/static/88685.png",
            2 => "/files/ps2/images/static/94469.png"
        };

        if (character.FactionID is FactionDefinition.NSO && character.PrestigeLevel == 0)
            iconUrl = null;


        Color color = character.FactionID switch
        {
            FactionDefinition.NC => Color.DodgerBlue,
            FactionDefinition.TR => Color.DarkRed,
            FactionDefinition.VS => Color.Purple,
            FactionDefinition.NSO => Color.LightGray
        };

        EmbedField battleRankField = new
        (
            "Battle Rank",
            character.BattleRank.Value + (character.PrestigeLevel > 0
                ? $"~{character.PrestigeLevel}" : string.Empty),
            true
        );

        EmbedField kdRatioField = new
        (
            "K/D",
            ((double)character.Kills.AllTime / character.Deaths.AllTime).ToString("F2"),
            true
        );

        EmbedField recentRatioField = new
        (
            "Recent K/D",
            ((double)character.Kills.Month.M01 / character.Deaths.Month.M01).ToString("F2"),
            true
        );

        EmbedField lastLoginField = new
        (
            "Last Login",
            Formatter.Timestamp(character.Times.LastLogin, TimestampStyle.ShortDateTime),
            true
        );

        EmbedField playtimeField = new
        (
            "Playtime",
            TimeSpan.FromMinutes(character.Times.MinutesPlayed).ToString(@"dd\d\ hh\h\ mm\m"),
            true
        );

        EmbedField createdAtField = new
        (
            "Created",
            Formatter.Timestamp(character.Times.Creation, TimestampStyle.ShortDate),
            true
        );

        Embed embed = new
        (
            title,
            default,
            description,
            default,
            default,
            color,
            default,
            default,
            iconUrl is null ? default(Remora.Rest.Core.Optional<IEmbedThumbnail>) : new EmbedThumbnail(iconUrl),
            default,
            default,
            default,
            new List<IEmbedField> {
                battleRankField,
                kdRatioField,
                recentRatioField,
                lastLoginField,
                playtimeField,
                createdAtField
            }
        );

        ActionRowComponent buttons = new
        (
            new IMessageComponent[] {
                new ButtonComponent
                (
                    ButtonComponentStyle.Link,
                    "Fisu Stats",
                    URL: "https://ps2.fisu.pw/player/?name=" + character.Name.First
                ),
                new ButtonComponent
                (
                    ButtonComponentStyle.Link,
                    "Honu Stats",
                    URL: "https://wt.honu.pw/c/" + character.CharacterID
                ),
                new ButtonComponent
                (
                    ButtonComponentStyle.Link,
                    "Voidwell Stats",
                    URL: $"https://voidwell.com/ps2/player/{character.CharacterID}/stats"
                )
            }
        );

        return await _feedbackService.SendContextualEmbedAsync
        (
            embed,
            new FeedbackMessageOptions(MessageComponents: new IMessageComponent[] { buttons }),
            CancellationToken
        );
    }
    #pragma warning restore CS8509
}
