using DbgCensus.EventStream.Abstractions.Objects.Events.Characters;
using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.Planetside.Abstractions.Services;

/// <summary>
/// Represents a service for collating and logging base capture events.
/// </summary>
public interface IFacilityCaptureService
{
    /// <summary>
    /// Gets a value indicating whether or not this <see cref="IFacilityCaptureService"/>
    /// is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Runs the resolver service. This method will not return until cancelled.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RunAsync(CancellationToken ct = default);

    /// <summary>
    /// Registers a facility control event.
    /// </summary>
    /// <param name="facilityControl">The facility control event.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the potentially asynchronous operation.</returns>
    ValueTask RegisterFacilityControlEventAsync(IFacilityControl facilityControl, CancellationToken ct = default);

    /// <summary>
    /// Registers a player facility capture event.
    /// </summary>
    /// <param name="playerFacilityCapture">The player facility capture event.</param>
    void RegisterPlayerFacilityCaptureEvent(IPlayerFacilityCapture playerFacilityCapture);
}
