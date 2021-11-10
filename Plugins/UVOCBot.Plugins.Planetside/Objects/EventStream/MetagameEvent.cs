using DbgCensus.Core.Objects;
using System;
using System.Text.Json.Serialization;
using UVOCBot.Plugins.Planetside.Objects.CensusCommon;

namespace UVOCBot.Plugins.Planetside.Objects.EventStream;

/// <summary>
/// Initialises a new instance of the <see cref="MetagameEvent"/> record.
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
    double ExperienceBonus,
    double FactionNC,
    double FactionTR,
    double FactionVS,
    [property: JsonPropertyName("metagame_event_id")] MetagameEventDefinition EventDefinition,
    [property: JsonPropertyName("metagame_event_state")] MetagameEventState EventState,
    ZoneId ZoneID
);
