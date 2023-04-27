using Microsoft.Extensions.Caching.Memory;
using System;
using UVOCBot.Plugins.Planetside.Objects.SanctuaryCensus;

namespace UVOCBot.Plugins.Planetside.Objects;

public static class CacheEntryHelpers
{
    /// <summary>
    /// Gets the memory cache options for the <see cref="CensusQuery.Outfit.Outfit"/> class.
    /// </summary>
    public static readonly MemoryCacheEntryOptions OutfitOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3),
        Priority = CacheItemPriority.Low,
        Size = 2
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="CensusQuery.Map.Map"/> class.
    /// </summary>
    public static readonly MemoryCacheEntryOptions MapOptions = new()
    {
        // Expect the map state to be kept up to date by the FacilityCaptureService
        // (max continent open time) * (number of continents in general rotation)
        SlidingExpiration = TimeSpan.FromHours(6.5 * 5),
        Priority = CacheItemPriority.High,
        Size = 4
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="SanctuaryCensus.MapRegion"/> class.
    /// </summary>
    public static readonly MemoryCacheEntryOptions MapRegionOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
        Priority = CacheItemPriority.High,
        Size = 1
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="CensusQuery.MinimalCharacter"/> class.
    /// </summary>
    public static readonly MemoryCacheEntryOptions MinimalCharacterOptions = new()
    {
        SlidingExpiration = TimeSpan.FromDays(1),
        Priority = CacheItemPriority.Low,
        Size = 1
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="Abstractions.Objects.IPopulation"/> interface.
    /// </summary>
    public static readonly MemoryCacheEntryOptions PopulationOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
        Priority = CacheItemPriority.Normal,
        Size = 1
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="SanctuaryCensus.OutfitWarRegistration"/> object.
    /// </summary>
    public static readonly MemoryCacheEntryOptions OutfitWarRegistrationsOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        Priority = CacheItemPriority.Normal,
        Size = 3
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="SanctuaryCensus.OutfitWar"/> object.
    /// </summary>
    public static readonly MemoryCacheEntryOptions OutfitWarOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
        Priority = CacheItemPriority.Normal,
        Size = 1
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="SanctuaryCensus.OutfitWarRoundWithMatches"/> object.
    /// </summary>
    public static MemoryCacheEntryOptions GetOutfitWarRoundWithMatchesOptions(OutfitWarRoundWithMatches round) =>
        new()
        {
            AbsoluteExpiration = round.EndTime,
            Priority = CacheItemPriority.Normal,
            Size = 1
        };

    /// <summary>
    /// Gets the memory cache options for the <see cref="SanctuaryCensus.ExperienceRank"/> object.
    /// </summary>
    /// <returns></returns>
    public static MemoryCacheEntryOptions GetExperienceRankOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
        Priority = CacheItemPriority.Low,
        Size = 1
    };
}
