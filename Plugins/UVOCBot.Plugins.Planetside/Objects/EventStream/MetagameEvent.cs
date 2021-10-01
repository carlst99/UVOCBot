using DbgCensus.Core.Objects;
using System.Text.Json.Serialization;

namespace UVOCBot.Plugins.Planetside.Objects.EventStream
{
    /// <summary>
    /// Enumerates the various states a <see cref="MetagameEvent"/> can represent.
    /// </summary>
    public enum MetagameEventState : uint
    {
        Started = 135,
        Restarted = 136,
        Canceled = 137,
        Ended = 138,
        XPBonusChanged = 139
    }

    /// <summary>
    /// Enumerates the various metagate event types.
    /// </summary>
    public enum MetagameEventDefinition : uint
    {
        TRMeltdownAmerish = 147,
        VSMeltdownAmerish = 148,
        NCMeltdownAmerish = 149,
        TRMeltdownEsamir = 150,
        VSMeltdownEsamir = 151,
        NCMeltdownEsamir = 152,
        TRMeltdownHossin = 153,
        VSMeltdownHossin = 154,
        NCMeltdownHossing = 155,
        TRMeltdownIndar = 156,
        VSMeltdownIndar = 157,
        NCMeltdownIndar = 158,
        LowPopCollapseAmerish = 159,
        LowPopCollapseEsamir = 160,
        LowPopCollapseHossin = 161,
        LowPopCollapseIndar = 162,
        NCUnstableMeltdownEsamir = 176,
        NCUnstableMeltdownHossin = 177,
        NCUnstableMeltdownAmerish = 178,
        NCUnstableMeltdownIndar = 179,
        VSUnstableMeltdownEsamir = 186,
        VSUnstableMeltdownHossin = 187,
        VSUnstableMeltdownAmerish = 188,
        VSUnstableMeltdownIndar = 189,
        TRUnstableMeltdownEsamir = 190,
        TRUnstableMeltdownHossin = 191,
        TRUnstableMeltdownAmerish = 192,
        TRUnstableMeltdownIndar = 193,
        OutfitWarsCaptureRelics = 204,
        OutfitWarsPreMatch = 205,
        OutfitWarsRelicsChanging = 206,
        OutfitWarsMatchStart = 207,
        NCMeltdownKoltyr = 208,
        TRMeltdownKoltyr = 209,
        VSMeltdownKoltyr = 210,
        ConquestAmerish = 211,
        ConquestEsamir = 212,
        ConquestHossin = 213,
        ConquestIndar = 214
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="MetagameEvent"/> class.
    /// </summary>
    /// <param name="EventName">The name of the event.</param>
    /// <param name="Timestamp">The time at which the event occured.</param>
    /// <param name="WorldID">The world on which the event occured.</param>
    /// <param name="ExperienceBonus">The (percentage-based?) XP bonus awarded for playing throughout the event.</param>
    /// <param name="FactionNC">The amount of territory held by the NC at the time of the event.</param>
    /// <param name="FactionTR">The amount of territory held by the TR at the time of the event.</param>
    /// <param name="FactionVS">The amount of territory held by the VS at the time of the event.</param>
    /// <param name="EventDefinition">The definition of the event.</param>
    /// <param name="EventState">The state of the event.</param>
    /// <param name="ZoneID">The zone on which the event occured.</param>
    public record MetagameEvent
    (
        string EventName,
        DateTimeOffset Timestamp,
        WorldDefinition WorldID,
        int ExperienceBonus,
        double FactionNC,
        double FactionTR,
        double FactionVS,
        [property: JsonPropertyName("metagame_event_id")] MetagameEventDefinition EventDefinition,
        [property: JsonPropertyName("metagame_event_state")] MetagameEventState EventState,
        ZoneId ZoneID
    )
    {
        public object GetCacheKey()
            => (typeof(MetagameEvent), (int)WorldID, (int)ZoneID.Definition);

        public static object GetCacheKey(WorldDefinition worldDefinition, ZoneDefinition zoneDefinition)
            => (typeof(MetagameEvent), (int)worldDefinition, (int)zoneDefinition);
    }
}
