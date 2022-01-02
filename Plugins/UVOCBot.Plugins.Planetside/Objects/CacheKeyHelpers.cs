using DbgCensus.Core.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using System;
using UVOCBot.Plugins.Planetside.Abstractions.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;

namespace UVOCBot.Plugins.Planetside.Objects;

public static class CacheKeyHelpers
{
    public static object GetMetagameEventKey(IMetagameEvent metagameEvent)
        => GetMetagameEventKey(metagameEvent.WorldID, metagameEvent.ZoneID.Definition);

    public static object GetMetagameEventKey(WorldDefinition worldDefinition, ZoneDefinition zoneDefinition)
        => (typeof(IMetagameEvent), (uint)worldDefinition, (uint)zoneDefinition);

    public static object GetOutfitKey(Outfit outfit)
        => GetOutfitKey(outfit.OutfitId);

    public static object GetOutfitKey(ulong outfitId)
        => (typeof(Outfit), outfitId);

    public static object GetFacilityMapRegionKey(MapRegion facilityRegion)
    {
        if (facilityRegion.FacilityID is null)
            throw new ArgumentNullException(nameof(facilityRegion.FacilityID), "Facility ID must not be null.");

        return GetFacilityMapRegionKey(facilityRegion.FacilityID.Value);
    }

    public static object GetFacilityMapRegionKey(ulong facilityID)
        => (typeof(MapRegion), facilityID);

    public static object GetMapKey(WorldDefinition world, Map map)
        => GetMapKey(world, map.ZoneID.Definition);

    public static object GetMapKey(WorldDefinition world, ZoneDefinition zone)
        => (typeof(Map), (int)world, (int)zone);

    public static object GetPopulationKey(IPopulation population)
        => GetPopulationKey(population.World);

    public static object GetPopulationKey(WorldDefinition world)
        => (typeof(IPopulation), (int)world);
}
