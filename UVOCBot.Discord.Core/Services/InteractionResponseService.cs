using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core.Abstractions.Services;

namespace UVOCBot.Discord.Core.Services;

/// <inheritdoc cref="IInteractionResponseService"/>
public class InteractionResponseService : IInteractionResponseService
{
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly InteractionContext _context;

    /// <inheritdoc />
    public bool HasResponded { get; protected set; }

    public InteractionResponseService
    (
        IDiscordRestInteractionAPI interactionApi,
        InteractionContext context
    )
    {
        _interactionApi = interactionApi;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> CreateModalResponse
    (
        InteractionModalCallbackData modalData,
        CancellationToken ct = default
    )
    {
        if (HasResponded)
            return new InvalidOperationError("A response has already been created for this scope.");

        Result responseResult = await _interactionApi.CreateInteractionResponseAsync
        (
            _context.ID,
            _context.Token,
            new InteractionResponse
            (
                InteractionCallbackType.Modal,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData,
                    IInteractionModalCallbackData>>(modalData)
            ),
            ct: ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }
    
    
}
