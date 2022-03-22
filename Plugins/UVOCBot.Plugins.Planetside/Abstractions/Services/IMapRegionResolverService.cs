using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Abstractions.Services;

/// <summary>
/// Represents a service for resolving <see cref="MapRegion"/>.
/// This is required as resolving individual map regions can need batching
/// when certain events, like a continent closing/opening, occur.
/// </summary>
public interface IMapRegionResolverService
{
    /// <summary>
    /// Gets a value indicating whether or not the resolver is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Runs the resolver service. This method will not return until cancelled.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RunAsync(CancellationToken ct = default);

    /// <summary>
    /// Queues a facility to be resolved into a map region.
    /// </summary>
    /// <param name="facilityControlEvent">The facility control event.</param>
    /// <param name="callback">The callback to invoke when the map region is resolved.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    ValueTask EnqueueAsync
    (
        IFacilityControl facilityControlEvent,
        Func<MapRegion, CancellationToken, Task> callback,
        CancellationToken ct = default
    );
}
