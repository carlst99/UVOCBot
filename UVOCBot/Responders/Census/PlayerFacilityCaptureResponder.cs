using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.EventStream;

namespace UVOCBot.Responders.Census
{
    public class PlayerFacilityCaptureResponder : ICensusEventHandler<ServiceMessage<PlayerFacilityCapture>>
    {
        public Task HandleAsync(ServiceMessage<PlayerFacilityCapture> censusEvent, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
