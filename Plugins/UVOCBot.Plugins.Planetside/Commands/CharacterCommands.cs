using DbgCensus.Core.Objects;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using DbgCensus.Rest.Objects;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
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

        Result<CharacterInfo?> characterResult = await _queryService.GetAsync<CharacterInfo>(query, CancellationToken);
        if (!characterResult.IsSuccess)
            return Result.FromError(characterResult);

        if (!characterResult.IsDefined(out CharacterInfo? character))
            return new GenericCommandError("That character doesn't exist");

        string title = character.OnlineStatus
            ? Formatter.Emoji("green_circle")
            : Formatter.Emoji("red_circle");

        title += " ";
        title += character.Title is null
            ? character.Name.First
            : character.Title.Name.English + " " + character.Name.First;

        string description = $"Of {character.WorldID}'s {character.FactionID}";

        string iconUrl = "https://census.daybreakgames.com" + character.PrestigeLevel switch
        {
            0 when character.FactionID is FactionDefinition.NC => character.BattleRank.Icons.NCImagePath,
            0 when character.FactionID is FactionDefinition.TR => character.BattleRank.Icons.TRImagePath,
            0 when character.FactionID is FactionDefinition.VS => character.BattleRank.Icons.VSImagePath,
            1 => "/files/ps2/images/static/88685.png",
            2 => "/files/ps2/images/static/94469.png"
        };


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
            "Created At",
            Formatter.Timestamp(character.Times.Creation, TimestampStyle.ShortDate),
            true
        );

        Embed embed = new
        (
            title,
            default,
            description,
            "https://ps2.fisu.pw/player/?name=" + character.Name.First,
            default,
            color,
            default,
            default,
            new EmbedThumbnail(iconUrl),
            default,
            default,
            default,
            new List<IEmbedField> {
                battleRankField,
                lastLoginField,
                playtimeField,
                createdAtField
            }
        );

        Result r = await _feedbackService.SendContextualEmbedAsync(embed, null, CancellationToken);
        return r;
    }
    #pragma warning restore CS8509
}
