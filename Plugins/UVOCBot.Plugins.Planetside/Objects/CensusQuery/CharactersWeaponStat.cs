namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery;

public record CharactersWeaponStat
(
    uint ItemID,
    CharactersWeaponStat.ItemInfo Info
)
{
    public record ItemInfo(GlobalisedString Name);
}
