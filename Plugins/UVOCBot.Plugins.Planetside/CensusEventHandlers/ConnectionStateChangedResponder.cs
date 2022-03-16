using DbgCensus.EventStream.Abstractions;
using DbgCensus.EventStream.Abstractions.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Control;
using DbgCensus.EventStream.Abstractions.Objects.Events;
using DbgCensus.EventStream.EventHandlers.Abstractions;
using DbgCensus.EventStream.EventHandlers.Abstractions.Objects;
using DbgCensus.EventStream.Objects.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.Planetside.CensusEventHandlers;

internal sealed class ConnectionStateChangedResponder : IPayloadHandler<IConnectionStateChanged>
{
    private static readonly Subscribe Subscription = new
    (
        new All(),
        new string[]
        {
                EventNames.FacilityControl,
                EventNames.MetagameEvent
        },
        Worlds: new All()
    );

    private readonly IEventStreamClientFactory _clientFactory;
    private readonly IPayloadContext _context;

    public ConnectionStateChangedResponder
    (
        IEventStreamClientFactory clientFactory,
        IPayloadContext context
    )
    {
        _clientFactory = clientFactory;
        _context = context;
    }

    public async Task HandleAsync(IConnectionStateChanged censusEvent, CancellationToken ct = default)
    {
        if (!censusEvent.Connected)
            return;

        IEventStreamClient client = _clientFactory.GetClient(_context.DispatchingClientName);
        await client.SendCommandAsync(Subscription, ct).ConfigureAwait(false);
    }
}
