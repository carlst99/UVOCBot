using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model.EventStream;

namespace UVOCBot.Responders.Census
{
    public class FacilityControlResponder : ICensusEventHandler<ServiceMessage<FacilityControl>>
    {
        private readonly ILogger<FacilityControlResponder> _logger;

        public FacilityControlResponder(ILogger<FacilityControlResponder> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(ServiceMessage<FacilityControl> censusEvent, CancellationToken ct = default)
        {
            FacilityControl controlEvent = censusEvent.Payload;

            return Task.CompletedTask;
        }
    }
}
