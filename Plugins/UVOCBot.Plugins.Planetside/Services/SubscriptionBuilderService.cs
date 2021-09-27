using DbgCensus.EventStream.Commands;
using UVOCBot.Plugins.Planetside.Services.Abstractions;

namespace UVOCBot.Plugins.Planetside.Services
{
    /// <inheritdoc cref="ISubscriptionBuilderService"/>
    public class SubscriptionBuilderService : ISubscriptionBuilderService
    {
        /// <inheritdoc />
        public virtual Task<SubscribeCommand> BuildAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new SubscribeCommand(
                new string[] { "all" },
                new string[] { EventStreamConstants.FACILITY_CONTROL_EVENT },
                worlds: new string[] { "all" }));
        }

        /// <inheritdoc />
        public Task RefreshAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
