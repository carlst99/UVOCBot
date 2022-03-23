using DbgCensus.Core.Objects;
using DbgCensus.Rest.Abstractions;
using DbgCensus.Rest.Abstractions.Queries;
using DbgCensus.Rest.Objects;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
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
    private const string FirstMonthKey = "m01";

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
    public async Task<Result> GetCharacterInfoCommandAsync([AutocompleteProvider("autocomplete::ps2CharacterName")] string characterName)
    {
        CharacterInfo? character = await GetCharacter(characterName);
        if (character is null)
            return new GenericCommandError("That character doesn't exist");

        CharactersWeaponStat? favWeapon = await GetFavouriteWeapon(character);

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

        EmbedField recentKDRatioField = new
        (
            "K/D",
            ((double)character.Kills.Month[FirstMonthKey] / character.Deaths.Month[FirstMonthKey]).ToString("F2"),
            true
        );

        EmbedField kdRatioField = new
        (
            "Lifetime K/D",
            ((double)character.Kills.AllTime / character.Deaths.AllTime).ToString("F2"),
            true
        );

        EmbedField kpmField = new
        (
            "KPM",
            (character.Kills.Month[FirstMonthKey] / ((double)character.Time.Month[FirstMonthKey] / 60)).ToString("F2"),
            true
        );

        EmbedField killCountField = new
        (
            "Lifetime Kills",
            character.Kills.AllTime.ToString(),
            true
        );

        EmbedField favWeaponField = new
        (
            "Favourite Weapon",
            favWeapon is null ? "Unknown" : favWeapon.Info.Name.English,
            true
        );

        EmbedField lastLoginField = new
        (
            "Last Login",
            Formatter.Timestamp(character.Times.LastLogin, TimestampStyle.RelativeTime),
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

        CharacterInfo.OutfitMemberExtended outfit = character.OutfitMember;
        EmbedField outfitField = new
        (
            "Outfit",
            Formatter.MaskedLink($"[{outfit.Alias}] {outfit.Name}", $"https://wt.honu.pw/o/{outfit.OutfitID}")
            + $"\nRank: {outfit.MemberRankOrdinal}. {outfit.MemberRank}",
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
            new EmbedFooter("If not otherwise specified, all stats are calculated from the last month of playtime"),
            default,
            iconUrl is null ? default(Remora.Rest.Core.Optional<IEmbedThumbnail>) : new EmbedThumbnail(iconUrl),
            default,
            default,
            default,
            new List<IEmbedField> {
                battleRankField,
                recentKDRatioField,
                kdRatioField,
                kpmField,
                killCountField,
                favWeaponField,
                lastLoginField,
                playtimeField,
                createdAtField,
                outfitField
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

    private async Task<CharacterInfo?> GetCharacter(string name)
    {
        IQueryBuilder characterQuery = _queryService.CreateQuery()
            .OnCollection("character")
            .WithLanguage(CensusLanguage.English)
            .Where("name.first_lower", SearchModifier.Equals, name.ToLower())
            .HideFields("certs", "daily_ribbon", "head_id", "profile_id")
            .AddResolve("online_status")
            .AddResolve("outfit_member_extended", "member_since", "member_rank", "member_rank_ordinal", "outfit_id", "name", "alias")
            .AddResolve("world");

        characterQuery.AddJoin("experience_rank")
            .OnField("battle_rank.value")
            .ToField("rank")
            .Where("vs.title.en", SearchModifier.NotEquals, "A.S.P. Operative")
            .InjectAt("icons")
            .ShowFields("vs_image_path", "nc_image_path", "tr_image_path");

        characterQuery.AddJoin("title")
            .InjectAt("title_info");

        characterQuery.AddJoin("characters_stat_history")
            .Where("stat_name", SearchModifier.Equals, "kills")
            .ShowFields("all_time", "month.m01")
            .InjectAt("kills");

        characterQuery.AddJoin("characters_stat_history")
            .Where("stat_name", SearchModifier.Equals, "deaths")
            .ShowFields("all_time", "month.m01")
            .InjectAt("deaths");

        characterQuery.AddJoin("characters_stat_history")
            .Where("stat_name", SearchModifier.Equals, "time")
            .ShowFields("month.m01")
            .InjectAt("time");

        return await _queryService.GetAsync<CharacterInfo>(characterQuery, CancellationToken);
    }

    private async Task<CharactersWeaponStat?> GetFavouriteWeapon(CharacterInfo character)
    {
        IQueryBuilder favWeaponQuery = _queryService.CreateQuery()
            .OnCollection("characters_weapon_stat")
            .Where("character_id", SearchModifier.Equals, character.CharacterID)
            .Where("stat_name", SearchModifier.Equals, "weapon_fire_count")
            .Where("item_id", SearchModifier.NotEquals, 0)
            .WithSortOrder("value", SortOrder.Descending)
            .ShowFields("item_id")
            .AddJoin("item", j =>
            {
                j.InjectAt("info")
                    .ShowFields("name");
            });

        return await _queryService.GetAsync<CharactersWeaponStat>(favWeaponQuery, CancellationToken);
    }
}
