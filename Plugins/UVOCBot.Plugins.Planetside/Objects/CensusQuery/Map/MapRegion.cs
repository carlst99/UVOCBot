using DbgCensus.Core.Objects;

namespace UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

/// <summary>
/// Initialises a new instance of the <see cref="MapRegion"/> record.
/// </summary>
/// <param name="MapRegionID">The ID of the map region.</param>
/// <param name="ZoneID">The ID of the zone that this region belongs to.</param>
/// <param name="FacilityID">The ID of the facility contained within this region.</param>
/// <param name="FacilityName">The name of the facility contained within this region.</param>
/// <param name="FacilityTypeID">The type of the facility contained within this region.</param>
/// <param name="FacilityType">The friendly name of the <see cref="FacilityTypeID"/> parameter.</param>
/// <param name="LocationX">The X-coordinate (left-to-right) of the facility on the map.</param>
/// <param name="LocationY">The Y-coordinate (height) of the facility on the map.</param>
/// <param name="LocationZ">The Z-coordinate (top-to-bottom) of the facility on the map.</param>
/// <param name="RewardAmount">The amount of currency rewarded for owning this base.</param>
/// <param name="RewardCurrencyID">The type of currency rewarded for owning this base.</param>
public record MapRegion
(
    uint MapRegionID,
    ZoneID ZoneID,
    uint? FacilityID,
    string FacilityName,
    FacilityType? FacilityTypeID,
    string? FacilityType,
    double? LocationX,
    double? LocationY,
    double? LocationZ,
    uint? RewardAmount,
    uint? RewardCurrencyID
);
