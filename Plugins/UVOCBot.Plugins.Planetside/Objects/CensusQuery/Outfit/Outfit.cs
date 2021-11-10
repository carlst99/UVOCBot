using System;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

/// <summary>
/// Initialises a new instance of the <see cref="Outfit"/> record.
/// </summary>
/// <param name="OutfitId">The unique ID of the outfit.</param>
/// <param name="Name">The name of the outfit.</param>
/// <param name="NameLower">The lowercase name of the outfit.</param>
/// <param name="Alias">The alias (tag) of the outfit.</param>
/// <param name="AliasLower">The lowercase alias (tag) of the outfit.</param>
/// <param name="TimeCreated">The datetime at which the outfit was created.</param>
/// <param name="LeaderCharacterId">The ID of the character that owns the outfit.</param>
/// <param name="MemberCount">The number of members in the outfit.</param>
public record Outfit
(
    ulong OutfitId,
    string Name,
    string NameLower,
    string Alias,
    string AliasLower,
    DateTimeOffset TimeCreated,
    ulong LeaderCharacterId,
    uint MemberCount
);
