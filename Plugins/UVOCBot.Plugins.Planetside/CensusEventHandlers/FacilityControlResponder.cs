using DbgCensus.EventStream.Abstractions.Objects.Events.Worlds;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class FacilityControlResponder : IPayloadHandler<IFacilityControl>
{
    private readonly IFacilityCaptureService _facilityCaptureService;

    public FacilityControlResponder(IFacilityCaptureService facilityCaptureService)
    {
        _facilityCaptureService = facilityCaptureService;
    }

    public async Task HandleAsync(IFacilityControl censusEvent, CancellationToken ct = default)
    {
        // No need to bother with same-faction control events (i.e. point defenses).
        if (censusEvent.OldFactionID == censusEvent.NewFactionID)
            return;

        await _facilityCaptureService.RegisterFacilityControlEventAsync(censusEvent, ct).ConfigureAwait(false);
    }
}
