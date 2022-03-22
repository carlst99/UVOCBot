using DbgCensus.Core.Objects;
using System;

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
    CharacterInfo.CharacterTitleInfo? Title,
    CharacterInfo.CharacterStatHistory Deaths,
    CharacterInfo.CharacterStatHistory Kills
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

    public record CharacterStatHistory(uint AllTime);
}
