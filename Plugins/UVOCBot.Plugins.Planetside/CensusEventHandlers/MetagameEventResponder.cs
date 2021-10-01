using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Event;
using UVOCBot.Plugins.Planetside.Objects.EventStream;
using UVOCBot.Plugins.Planetside.Services;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers
{
    internal sealed class MetagameEventResponder : ICensusEventHandler<ServiceMessage<MetagameEvent>>
    {
        private readonly MetagameEventInjectionService _eventInjector;

        public MetagameEventResponder(MetagameEventInjectionService eventInjector)
        {
            _eventInjector = eventInjector;
        }

        public Task HandleAsync(ServiceMessage<MetagameEvent> censusEvent, CancellationToken ct = default)
        {
            _eventInjector.Set(censusEvent.Payload);

            return Task.CompletedTask;
        }
    }
}
