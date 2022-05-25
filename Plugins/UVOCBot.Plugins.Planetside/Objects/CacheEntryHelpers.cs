using Microsoft.Extensions.Caching.Memory;
using System;

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
        SlidingExpiration = TimeSpan.FromHours(1), // Expect the map state to be kept up to date by the FacilityCaptureService
        Priority = CacheItemPriority.High,
        Size = 4
    };

    /// <summary>
    /// Gets the memory cache options for the <see cref="CensusQuery.Map.MapRegion"/> class.
    /// </summary>
    public static readonly MemoryCacheEntryOptions MapRegionOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
        Priority = CacheItemPriority.Low,
        Size = 2
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
}
