using UVOCBot.Plugins.Planetside.Objects.CensusQuery;

namespace UVOCBot.Plugins.Planetside.Commands;

public record CharactersWeaponStat
(
    uint ItemID,
    CharactersWeaponStat.ItemInfo Info
)
{
    public record ItemInfo(GlobalisedString Name);
}
