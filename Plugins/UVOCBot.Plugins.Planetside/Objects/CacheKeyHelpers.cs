using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using System;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Objects;

public static class CacheKeyHelpers
{
    public static object GetMetagameEventKey(IMetagameEvent metagameEvent)
        => GetMetagameEventKey(metagameEvent.WorldID, metagameEvent.ZoneID.Definition);

    public static object GetMetagameEventKey(WorldDefinition worldDefinition, ZoneDefinition zoneDefinition)
        => (typeof(IMetagameEvent), (uint)worldDefinition, (uint)zoneDefinition);

    public static object GetMinimalCharacterKey(ulong characterID)
        => (typeof(MinimalCharacter), characterID);

    public static object GetMinimalCharacterKey(MinimalCharacter character)
        => (typeof(MinimalCharacter), character.CharacterID);

    public static object GetOutfitKey(Outfit outfit)
        => GetOutfitKey(outfit.OutfitId);

    public static object GetOutfitKey(ulong outfitId)
        => (typeof(Outfit), outfitId);

    public static object GetFacilityMapRegionKey(MapRegion facilityRegion)
    {
        if (facilityRegion.FacilityId is null)
            throw new ArgumentNullException(nameof(facilityRegion.FacilityId), "Facility ID must not be null.");

        return GetFacilityMapRegionKey(facilityRegion.FacilityId.Value);
    }

    public static object GetFacilityMapRegionKey(ulong facilityID)
        => (typeof(MapRegion), facilityID);

    public static object GetMapKey(WorldDefinition world, Map map)
        => GetMapKey(world, map.ZoneID.Definition);

    public static object GetMapKey(WorldDefinition world, ZoneDefinition zone)
        => (typeof(Map), (int)world, (int)zone);

    public static object GetPopulationKey(IPopulation population)
        => GetPopulationKey(population.WorldID);

    public static object GetPopulationKey(WorldDefinition world)
        => (typeof(IPopulation), (int)world);

    public static object GetOutfitWarRegistrationsKey(uint outfitWarID)
        => (typeof(OutfitWarRegistration), outfitWarID);

    public static object GetOutfitWarKey(ValidWorldDefinition world)
        => (typeof(OutfitWar), world);

    public static object GetOutfitWarRoundWithMatchesKey(uint outfitWarID)
        => (typeof(OutfitWarRoundWithMatches), outfitWarID);
}
