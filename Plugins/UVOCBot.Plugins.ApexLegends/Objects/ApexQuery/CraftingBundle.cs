using System.Collections.Generic;

namespace UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

public record CraftingBundle
(
    string Bundle,
    long Start,
    long End,
    CraftingBundleType BundleType,
    IReadOnlyList<CraftingBundleContent> BundleContent
);

public record CraftingBundleContent
(
    string Item,
    int Cost,
    CraftingBundleItemDetails ItemType
);

public record CraftingBundleItemDetails
(
    string Name,
    CraftingBundleRarity Rarity,
    string Asset,
    string RarityHex
);

public enum CraftingBundleType
{
    Daily = 0,
    Weekly= 1,
    Permanent = 2
}

public enum CraftingBundleRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}
