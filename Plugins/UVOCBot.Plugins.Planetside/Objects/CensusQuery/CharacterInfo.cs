using DbgCensus.Core.Objects;
using System;
using System.Collections.Generic;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

public record CharacterInfo
(
    ulong CharacterID,
    Name Name,
    FactionDefinition FactionID,
    uint TitleID,
    CharacterInfo.CharacterTimes Times,
    CharacterInfo.CharacterBattleRank BattleRank,
    short PrestigeLevel,
    WorldDefinition WorldID,
    bool OnlineStatus,
    CharacterInfo.CharacterTitleInfo? TitleInfo,
    CharacterInfo.CharacterStatHistory Deaths,
    CharacterInfo.CharacterStatHistory Kills,
    CharacterInfo.CharacterStatHistory Time,
    CharacterInfo.OutfitMemberExtended? OutfitMember
)
{
    public record CharacterTimes
    (
        DateTimeOffset Creation,
        DateTimeOffset LastSave,
        DateTimeOffset LastLogin,
        uint LoginCount,
        uint MinutesPlayed
    );

    public record CharacterBattleRank
    (
        float PercentToNext,
        uint Value,
        CharacterInfo.BattleRankIcons Icons
    );

    public record BattleRankIcons
    (
        string VSImagePath,
        string NCImagePath,
        string TRImagePath
    );

    public record CharacterTitleInfo
    (
        uint TitleID,
        GlobalisedString Name
    );

    public record CharacterStatHistory
    (
        uint AllTime,
        Dictionary<string, uint> Month
    );

    public record OutfitMemberExtended
    (
        ulong OutfitID,
        DateTimeOffset MemberSince,
        string MemberRank,
        int MemberRankOrdinal,
        string Name,
        string Alias
    );
}
