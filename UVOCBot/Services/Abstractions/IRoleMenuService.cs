using Remora.Results;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Services.Abstractions
{
    public interface IRoleMenuService
    {
        Task<Result> ToggleRolesAsync(CancellationToken ct = default);
        Task<Result> ConfirmRemoveRolesAsync(CancellationToken ct = default);
    }
}
