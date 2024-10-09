using DbgCensus.Core.Objects;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

/// <summary>
/// Represents map region data.
/// </summary>
/// <param name="MapRegionId">The ID of the region.</param>
/// <param name="ZoneId">The ID of the zone that this region is on.</param>
/// <param name="FacilityId">The ID of the facility within this region.</param>
/// <param name="FacilityName">The name of the facility contained within the region.</param>
/// <param name="LocalizedFacilityName">The localized name of the facility contained within the region.</param>
/// <param name="FacilityTypeId">The ID of the type of the facility.</param>
/// <param name="FacilityType">The name of the type of the facility.</param>
/// <param name="LocationX">The X coordinate of the center of the region.</param>
/// <param name="LocationY">The Y coordinate of the center of the region.</param>
/// <param name="LocationZ">The Z coordinate of the center of the region.</param>
/// <param name="OutfitResourceRewardTypeDescription">The type of the Outfit Resource rewarded by this facility upon capture.</param>
/// <param name="OutfitResourceRewardAmount">The amount of the Outfit Resource rewarded by this facility upon capture.</param>
public record MapRegion
(
    uint MapRegionId,
    uint ZoneId,
    uint? FacilityId,
    string? FacilityName,
    GlobalizedString? LocalizedFacilityName,
    FacilityType FacilityTypeId,
    string? FacilityType,
    decimal? LocationX,
    decimal? LocationY,
    decimal? LocationZ,
    string? OutfitResourceRewardTypeDescription,
    int? OutfitResourceRewardAmount
);
