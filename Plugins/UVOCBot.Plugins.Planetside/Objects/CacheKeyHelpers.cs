using DbgCensus.Core.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Outfit;
using UVOCBot.Plugins.Planetside.Objects.EventStream;

namespace UVOCBot.Plugins.Planetside.Objects
{
    public static class CacheKeyHelpers
    {
        public static object GetMetagameEventKey(MetagameEvent metagameEvent)
            => GetMetagameEventKey(metagameEvent.WorldID, metagameEvent.ZoneID.Definition);

        public static object GetMetagameEventKey(WorldDefinition worldDefinition, ZoneDefinition zoneDefinition)
            => (typeof(MetagameEvent), (uint)worldDefinition, (uint)zoneDefinition);

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
            => GetMapKey(world, map.ZoneId.Definition);

        public static object GetMapKey(WorldDefinition world, ZoneDefinition zone)
            => (typeof(Map), (int)world, (int)zone);
    }
}
