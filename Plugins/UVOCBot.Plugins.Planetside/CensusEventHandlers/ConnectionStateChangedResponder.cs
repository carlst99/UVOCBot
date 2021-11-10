using DbgCensus.EventStream.Abstractions;
using DbgCensus.EventStream.Commands;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Objects.Push;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class ConnectionStateChangedResponder : ICensusEventHandler<ConnectionStateChanged>
{
    private readonly IEventStreamClientFactory _clientFactory;
    private readonly ISubscriptionBuilderService _subscriptionBuilder;

    public ConnectionStateChangedResponder(IEventStreamClientFactory clientFactory, ISubscriptionBuilderService subscriptionBuilder)
    {
        _clientFactory = clientFactory;
        _subscriptionBuilder = subscriptionBuilder;
    }

    public async Task HandleAsync(ConnectionStateChanged censusEvent, CancellationToken ct = default)
    {
        if (!censusEvent.Connected)
            return;

        IEventStreamClient client = _clientFactory.GetClient(censusEvent.DispatchingClientName);

        SubscribeCommand subscription = await _subscriptionBuilder.BuildAsync(ct).ConfigureAwait(false);
        await client.SendCommandAsync(subscription, ct).ConfigureAwait(false);
    }
}
