using DbgCensus.EventStream.Abstractions.Objects.Events.Characters;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class PlayerFacilityCaptureResponder : IPayloadHandler<IPlayerFacilityCapture>
{
    private readonly IFacilityCaptureService _facilityCaptureService;

    public PlayerFacilityCaptureResponder(IFacilityCaptureService facilityCaptureService)
    {
        _facilityCaptureService = facilityCaptureService;
    }

    public Task HandleAsync(IPlayerFacilityCapture censusEvent, CancellationToken ct = default)
    {
        _facilityCaptureService.RegisterPlayerFacilityCaptureEvent(censusEvent);
        return Task.CompletedTask;
    }
}
