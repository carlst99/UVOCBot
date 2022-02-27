using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
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
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(modalData)
            ),
            ct: ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }

    /// <inheritdoc />
    public async Task<Result> CreateMessageResponse
    (
        InteractionMessageCallbackData message,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
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
                InteractionCallbackType.DeferredChannelMessageWithSource,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(message)
            ),
            attachments,
            ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }

    /// <inheritdoc />
    public async Task<Result> CreateDeferredMessageResponse
    (
        bool isEphemeral,
        CancellationToken ct = default
    )
    {
        if (HasResponded)
            return new InvalidOperationError("A response has already been created for this scope.");

        Optional<MessageFlags> flags = isEphemeral
            ? MessageFlags.Ephemeral
            : default;

        Result responseResult = await _interactionApi.CreateInteractionResponseAsync
        (
            _context.ID,
            _context.Token,
            new InteractionResponse
            (
                InteractionCallbackType.DeferredChannelMessageWithSource,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>
                (
                    new InteractionMessageCallbackData(Flags: flags)
                )
            ),
            ct: ct
        );

        if (responseResult.IsSuccess)
            HasResponded = true;

        return responseResult;
    }

    /// <inheritdoc />
    public async Task<Result> CreateContextualMessageResponse
    (
        InteractionMessageCallbackData message,
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default,
        CancellationToken ct = default
    )
    {
        if (!HasResponded)
            return await CreateMessageResponse(message, attachments, ct);

        Result<IMessage> responseResult = await _interactionApi.CreateFollowupMessageAsync
        (
            DiscordConstants.ApplicationId,
            _context.Token,
            message.Content,
            message.IsTTS,
            message.Embeds,
            message.AllowedMentions,
            message.Components,
            attachments,
            message.Flags,
            ct
        );

        return responseResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(responseResult);
    }
}
