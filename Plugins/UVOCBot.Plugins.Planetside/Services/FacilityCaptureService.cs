using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;
using UVOCBot.Plugins.Planetside.Objects.CensusQuery.Map;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="IFacilityCaptureService" />
public sealed class FacilityCaptureService : IFacilityCaptureService
{
    private readonly ILogger<FacilityCaptureService> _logger;
    private readonly ICensusApiService _censusApiService;
    private readonly Channel<(IFacilityControl ControlEvent, Func<MapRegion, CancellationToken, Task> Callback)> _resolveQueue;

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FacilityCaptureService"/> class.
    /// </summary>
    /// <param name="logger">The logging provider.</param>
    /// <param name="censusApiService">The Census API query service.</param>
    public FacilityCaptureService(ILogger<FacilityCaptureService> logger, ICensusApiService censusApiService)
    {
        _logger = logger;
        _censusApiService = censusApiService;

        _resolveQueue = Channel.CreateUnbounded<(IFacilityControl ControlEvent, Func<MapRegion, CancellationToken, Task> Callback)>();
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (IsRunning)
            throw new InvalidOperationException("Already running");

        IsRunning = true;

        await foreach ((IFacilityControl cEvent, Func<MapRegion, CancellationToken, Task> callback) in _resolveQueue.Reader.ReadAllAsync(ct))
        {
            Result<MapRegion?> regionResult = await _censusApiService.GetFacilityRegionAsync(cEvent.FacilityID, ct);
            if (!regionResult.IsDefined(out MapRegion? region))
            {
                if (regionResult.Error is ExceptionError { Exception: BrokenCircuitException })
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);

                await _resolveQueue.Writer.WriteAsync((cEvent, callback), ct);
                continue;
            }

            try
            {
                DateTimeOffset startCallbackTime = DateTimeOffset.UtcNow;
                await callback(region, ct);
                TimeSpan executionTime = DateTimeOffset.UtcNow - startCallbackTime;

                TimeSpan delayTime = TimeSpan.FromMilliseconds(100) - executionTime;
                if (delayTime > TimeSpan.Zero)
                    await Task.Delay(delayTime, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure when invoking map region resolved callback");
                await Task.Delay(100, ct);
            }
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync
    (
        IFacilityControl facilityControlEvent,
        Func<MapRegion, CancellationToken, Task> callback,
        CancellationToken ct = default
    )
        => _resolveQueue.Writer.WriteAsync((facilityControlEvent, callback), ct);
}
