using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

public record StatsBridge
(
    StatsBridgeGlobal Global,
    StatsBridgeRealtime Realtime,
    StatsBridgeLegends Legends,
    StatsBridgeTotal Total
);

public record StatsBridgeGlobal
(
    string Name,
    ulong Uid,
    string Avatar,
    string Platform,
    int Level,
    int ToNextLevelPercent,
    StatsBridgeGlobal.BanInfo Bans,
    StatsBridgeGlobal.RankInfo Rank,
    StatsBridgeGlobal.RankInfo Arena,
    int LevelPrestige
)
{
    public record BanInfo
    (
        bool IsActive,
        int RemainingSeconds,
        [property: JsonPropertyName("last_banReason")] string LastBanReason
    );

    public record RankInfo
    (
        int RankScore,
        string RankName,
        int RankDiv,
        int LadderPosPlatform,
        string RankImg,
        string RankedSeason
    );
}

public record StatsBridgeRealtime
(
    string? LobbyState,
    byte IsOnline,
    byte IsInGame,
    byte CanJoin,
    byte PartyFull,
    string SelectedLegend,
    string CurrentState,
    long CurrentStateSinceTimestamp,
    int? CurrentStateSecsAgo,
    string CurrentStateAsText
);

public record StatsBridgeLegends
(
    StatsBridgeLegends.FullLegendData Selected,
    Dictionary<string, StatsBridgeLegends.MinimalLegendData> All
)
{
    public record FullLegendData
    (
        string LegendName,
        LegendImageAssets ImgAssets
    );

    public record MinimalLegendData(LegendImageAssets ImgAssetses);

    public record LegendImageAssets(string Icon, string Banner);
}

public record StatsBridgeTotal
(
    Dictionary<string, StatsBridgeTotal.Tracker> Trackers
)
{
    public record Tracker(string Name, string Value);
}
