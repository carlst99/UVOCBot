using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Discord.Core.Components;

/// <summary>
/// Represents a responder for component interactions.
/// </summary>
public interface IComponentResponder
{
    Task<Result> RespondAsync(string? dataFragment, CancellationToken ct = default);
}
