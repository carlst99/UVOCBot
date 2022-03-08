using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Objects.Events.Worlds;
using System;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

/// <summary>
/// Initialises a new instance of the <see cref="QueryMetagameEvent"/> record.
/// </summary>
/// <param name="ExperienceBonus">The (percentage-based?) XP bonus awarded for playing throughout the event.</param>
/// <param name="EventType">The type of the event.</param>
/// <param name="FactionNC">The amount of territory held by the NC at the time of the event.</param>
/// <param name="FactionTR">The amount of territory held by the TR at the time of the event.</param>
/// <param name="FactionVS">The amount of territory held by the VS at the time of the event.</param>
/// <param name="InstanceID">The instance of the zone that the event occured on.</param>
/// <param name="MetagameEventID">The definition of the event.</param>
/// <param name="MetagameEventState">The state of the event.</param>
/// <param name="MetagameEventStateName">A string representation of the <see cref="MetagameEventState" />.</param>
/// <param name="TableType">The Census query table that this event was retrieved from before display in the world_event collection.</param>
/// <param name="Timestamp">The time at which the event occured.</param>
/// <param name="WorldID">The world that the event occured on.</param>
/// <param name="ZoneID">The zone that the event occured on.</param>
public record QueryMetagameEvent
(
    double ExperienceBonus,
    string EventType,
    double FactionNC,
    double FactionTR,
    double FactionVS,
    ushort InstanceID,
    MetagameEventDefinition MetagameEventID,
    MetagameEventState MetagameEventState,
    string MetagameEventStateName,
    string TableType,
    DateTimeOffset Timestamp,
    WorldDefinition WorldID,
    ZoneDefinition ZoneID
)
{
    public MetagameEvent ToEventStreamMetagameEvent()
        => new
        (
            ExperienceBonus,
            EventType,
            FactionNC,
            FactionTR,
            FactionVS,
            InstanceID,
            MetagameEventID,
            MetagameEventState,
            MetagameEventStateName,
            Timestamp,
            WorldID,
            new ZoneID(ZoneID, InstanceID)
        );
}
