using DbgCensus.EventStream.Abstractions.Objects.Commands;
using DbgCensus.EventStream.Abstractions.Objects.Events;
using DbgCensus.EventStream.Objects.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

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
                new string[] { "all" },
                new string[]
                {
                        EventNames.FacilityControl,
                        EventNames.MetagameEvent
                },
                Worlds: new string[] { "all" }
            )
        );

    /// <inheritdoc />
    public Task RefreshAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
