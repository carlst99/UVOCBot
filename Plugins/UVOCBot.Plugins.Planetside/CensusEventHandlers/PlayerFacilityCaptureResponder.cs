using DbgCensus.EventStream.Abstractions.Objects.Events.Characters;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class PlayerFacilityCaptureResponder : IPayloadHandler<IPlayerFacilityCapture>
{
    public Task HandleAsync(IPlayerFacilityCapture censusEvent, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
