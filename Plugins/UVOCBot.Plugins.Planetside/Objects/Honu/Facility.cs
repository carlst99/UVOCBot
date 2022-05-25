using DbgCensus.Core.Objects;
using System.Text.Json.Serialization;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Objects.Honu;

/// <summary>
/// Represents facility information provided by Honu's API.
/// <see href="https://wt.honu.pw/api/map/facilities"/>.
/// </summary>
/// <param name="FacilityID">The ID of the facility.</param>
/// <param name="ZoneID">The zone that the facility is in.</param>
/// <param name="RegionID">The region that the facility is in.</param>
/// <param name="Name">The name of the facility.</param>
/// <param name="TypeID">The type of the facility.</param>
/// <param name="TypeName">The type name of the facility.</param>
/// <param name="LocationX">The x-coordinate of the facility.</param>
/// <param name="LocationY">The y-coordinate of the facility.</param>
/// <param name="LocationZ">The z-coordinate of the facility.</param>
public record Facility
(
    uint FacilityID,
    ushort ZoneID,
    uint RegionID,
    string Name,
    int TypeID,
    string TypeName,
    double? LocationX,
    double? LocationY,
    double? LocationZ
);
