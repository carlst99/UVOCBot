using DbgCensus.EventStream.Abstractions;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Push;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Responders.Census
{
    public class ConnectionStateChangedResponder : ICensusEventHandler<ConnectionStateChanged>
    {
        private readonly IEventStreamClientFactory _clientFactory;

        public ConnectionStateChangedResponder(IEventStreamClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task HandleAsync(ConnectionStateChanged censusEvent, CancellationToken ct = default)
        {
            if (!censusEvent.Connected)
                return;

            IEventStreamClient client = _clientFactory.GetClient(censusEvent.DispatchingClientName);
            await client.SendCommandAsync(BotConstants.CORE_CENSUS_SUBSCRIPTION, ct).ConfigureAwait(false);
        }
    }
}
