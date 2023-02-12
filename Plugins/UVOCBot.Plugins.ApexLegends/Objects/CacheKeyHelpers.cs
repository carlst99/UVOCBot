using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Objects;

public static class CacheKeyHelpers
{
    public static object GetMapRotationBundleKey()
        => typeof(MapRotationBundle);

    public static object GetCraftingBundleKey()
        => typeof(CraftingBundle);
}
