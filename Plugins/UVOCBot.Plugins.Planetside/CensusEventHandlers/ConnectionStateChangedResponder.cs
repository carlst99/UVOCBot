using DbgCensus.EventStream.Abstractions;
using DbgCensus.EventStream.Abstractions.Objects.Commands;
using DbgCensus.EventStream.Abstractions.Objects.Control;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Abstractions.Objects;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class ConnectionStateChangedResponder : IPayloadHandler<IConnectionStateChanged>
{
    private readonly IEventStreamClientFactory _clientFactory;
    private readonly ISubscriptionBuilderService _subscriptionBuilder;
    private readonly IPayloadContext _context;

    public ConnectionStateChangedResponder
    (
        IEventStreamClientFactory clientFactory,
        ISubscriptionBuilderService subscriptionBuilder,
        IPayloadContext context
    )
    {
        _clientFactory = clientFactory;
        _subscriptionBuilder = subscriptionBuilder;
        _context = context;
    }

    public async Task HandleAsync(IConnectionStateChanged censusEvent, CancellationToken ct = default)
    {
        if (!censusEvent.Connected)
            return;

        IEventStreamClient client = _clientFactory.GetClient(_context.DispatchingClientName);

        ISubscribe subscription = await _subscriptionBuilder.BuildAsync(ct).ConfigureAwait(false);
        await client.SendCommandAsync(subscription, ct).ConfigureAwait(false);
    }
}
