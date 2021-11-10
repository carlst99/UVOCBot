using DbgCensus.Core.Objects;
using System;
using UVOCBot.Plugins.Planetside.Objects.CensusCommon;
using UVOCBot.Plugins.Planetside.Objects.EventStream;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

/// <summary>
/// Initialises a new instance of the <see cref="QueryMetagameEvent"/> record.
/// </summary>
/// <param name="MetagameEventID">The definition of the event.</param>
/// <param name="MetagameEventState">The state of the event.</param>
/// <param name="FactionNC">The amount of territory held by the NC at the time of the event.</param>
/// <param name="FactionTR">The amount of territory held by the TR at the time of the event.</param>
/// <param name="FactionVS">The amount of territory held by the VS at the time of the event.</param>
/// <param name="ExperienceBonus">The (percentage-based?) XP bonus awarded for playing throughout the event.</param>
/// <param name="Timestamp">The time at which the event occured.</param>
/// <param name="ZoneID">The zone that the event occured on.</param>
/// <param name="WorldID">The world that the event occured on.</param>
/// <param name="EventType">The type of the event.</param>
/// <param name="TableType">The Census query table that this event was retrieved from before display in the world_event collection.</param>
/// <param name="InstanceID">The instance of the zone that the event occured on.</param>
/// <param name="MetagameEventStateName">A string representation of the <see cref="MetagameEventState" />.</param>
public record QueryMetagameEvent
(
    MetagameEventDefinition MetagameEventID,
    MetagameEventState MetagameEventState,
    double FactionNC,
    double FactionTR,
    double FactionVS,
    double ExperienceBonus,
    DateTimeOffset Timestamp,
    ZoneDefinition ZoneID,
    WorldDefinition WorldID,
    string EventType,
    string TableType,
    ushort InstanceID,
    string MetagameEventStateName
)
{
    public MetagameEvent ToEventStreamMetagameEvent()
    {
        return new MetagameEvent
        (
            EventType,
            Timestamp,
            WorldID,
            ExperienceBonus,
            FactionNC,
            FactionTR,
            FactionVS,
            MetagameEventID,
            MetagameEventState,
            new ZoneId(ZoneID, InstanceID)
        );
    }
}
