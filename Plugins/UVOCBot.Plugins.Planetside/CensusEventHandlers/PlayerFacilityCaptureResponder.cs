using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using UVOCBot.Plugins.Planetside.Objects.EventStream;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers
{
    internal sealed class PlayerFacilityCaptureResponder : ICensusEventHandler<ServiceMessage<PlayerFacilityCapture>>
    {
        public Task HandleAsync(ServiceMessage<PlayerFacilityCapture> censusEvent, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
