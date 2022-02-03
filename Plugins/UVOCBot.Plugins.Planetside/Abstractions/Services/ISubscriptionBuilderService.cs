using DbgCensus.EventStream.Abstractions.Objects.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.Planetside.Abstractions.Services;

/// <summary>
/// Defines functions to build a subscription for the Census Event Stream.
/// </summary>
public interface ISubscriptionBuilderService
{
    /// <summary>
    /// Builds a subscription.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<ISubscribe> BuildAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces the subscription to be refreshed.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RefreshAsync(CancellationToken ct = default);
}
