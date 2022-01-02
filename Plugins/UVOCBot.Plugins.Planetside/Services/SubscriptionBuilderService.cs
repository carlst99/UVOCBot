using DbgCensus.EventStream.Abstractions.Objects;
using DbgCensus.EventStream.Abstractions.Objects.Commands;
using DbgCensus.EventStream.Abstractions.Objects.Events;
using DbgCensus.EventStream.Objects.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Abstractions.Services;

namespace UVOCBot.Plugins.Planetside.Services;

/// <inheritdoc cref="ISubscriptionBuilderService"/>
public class SubscriptionBuilderService : ISubscriptionBuilderService
{
    /// <inheritdoc />
    public virtual Task<ISubscribe> BuildAsync(CancellationToken ct = default)
        => Task.FromResult
        (
            (ISubscribe)new Subscribe
            (
                new All(),
                new string[]
                {
                        EventNames.FacilityControl,
                        EventNames.MetagameEvent
                },
                Worlds: new All()
            )
        );

    /// <inheritdoc />
    public Task RefreshAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}
