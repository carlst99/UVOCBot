using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using Polly.CircuitBreaker;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="IMapRegionResolverService" />
public sealed class MapRegionResolverService : IMapRegionResolverService
{
    private readonly ICensusApiService _censusApiService;
    private readonly Channel<(IFacilityControl ControlEvent, Func<MapRegion, Task> Callback)> _resolveQueue;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapRegionResolverService"/> class.
    /// </summary>
    /// <param name="censusApiService">The Census API query service.</param>
    public MapRegionResolverService(ICensusApiService censusApiService)
    {
        _censusApiService = censusApiService;

        _resolveQueue = Channel.CreateUnbounded<(IFacilityControl ControlEvent, Func<MapRegion, Task> Callback)>();
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (IsRunning)
            throw new InvalidOperationException("Already running");

        IsRunning = true;

        await foreach ((IFacilityControl cEvent, Func<MapRegion, Task> callback) in _resolveQueue.Reader.ReadAllAsync(ct))
        {
            Result<MapRegion?> regionResult = await _censusApiService.GetFacilityRegionAsync(cEvent.FacilityID, ct);
            if (!regionResult.IsDefined(out MapRegion? region))
            {
                if (regionResult.Error is ExceptionError {Exception: BrokenCircuitException})
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);

                await _resolveQueue.Writer.WriteAsync((cEvent, callback), ct);
                continue;
            }

            DateTimeOffset startCallbackTime = DateTimeOffset.UtcNow;
            await callback(region);
            TimeSpan executionTime = DateTimeOffset.UtcNow - startCallbackTime;

            TimeSpan delayTime = TimeSpan.FromMilliseconds(100) - executionTime;
            if (delayTime > TimeSpan.Zero)
                await Task.Delay(delayTime, ct);
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync
    (
        IFacilityControl facilityControlEvent,
        Func<MapRegion, Task> callback,
        CancellationToken ct = default
    )
        => _resolveQueue.Writer.WriteAsync((facilityControlEvent, callback), ct);
}
