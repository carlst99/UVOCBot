using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using UVOCBot.Discord.Core.Commands;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Plugins.Roles.Abstractions.Services;

namespace UVOCBot.Plugins.Roles.Responders;

internal sealed class EditMenuModalResponder : IComponentResponder
{
    private readonly IRoleMenuService _roleMenuService;
    private readonly IInteraction _context;
    private readonly DiscordContext _dbContext;
    private readonly FeedbackService _feedbackService;

    public EditMenuModalResponder
    (
        IRoleMenuService roleMenuService,
        IInteractionContext context,
        DiscordContext dbContext,
        FeedbackService feedbackService
    )
    {
        _roleMenuService = roleMenuService;
        _context = context.Interaction;
        _dbContext = dbContext;
        _feedbackService = feedbackService;
    }

    /// <inheritdoc />
    public Result<Attribute[]> GetResponseAttributes(string key)
        => Array.Empty<Attribute>();

    /// <inheritdoc />
    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
    {
        if (!ulong.TryParse(dataFragment, out ulong roleMenuMessageID))
            return Result.FromError(new GenericCommandError());

        Result validationResult = ValidateInteraction(out string titleText, out string? descriptionText);
        if (!validationResult.IsSuccess)
            return validationResult;

        if (!_roleMenuService.TryGetGuildRoleMenu(roleMenuMessageID, out GuildRoleMenu? menu))
            return await _feedbackService.SendContextualErrorAsync("That role menu doesn't exist.", ct: ct);

        menu.Title = titleText;
        menu.Description = descriptionText ?? string.Empty;

        _dbContext.Update(menu);
        int updateCount = await _dbContext.SaveChangesAsync(ct);

        if (updateCount < 1)
            return Result.FromError(new GenericCommandError());

        IResult modifyMenuResult = await _roleMenuService.UpdateRoleMenuMessageAsync(menu, ct);
        if (!modifyMenuResult.IsSuccess)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                "The menu was updated internally, but I couldn't update the corresponding message. " +
                $"Please use the {Formatter.InlineQuote("rolemenu update")} command.",
                ct: ct
            );
        }

        return await _feedbackService.SendContextualSuccessAsync("The role menu was successfully edited!", ct: ct).ConfigureAwait(false);
    }

    private Result ValidateInteraction
    (
        out string titleText,
        out string? descriptionText
    )
    {
        titleText = string.Empty;
        descriptionText = null;

        if (!_context.Data.Value.TryPickT2(out IModalSubmitData modalData, out _))
            return Result.FromError(new GenericCommandError());

        List<IPartialTextInputComponent> textInputs = modalData.Components
            .SelectMany(c => ((IPartialActionRowComponent)c).Components.Value)
            .OfType<IPartialTextInputComponent>()
            .ToList();

        IPartialTextInputComponent? titleComponent = textInputs.FirstOrDefault(c => c.CustomID == RoleComponentKeys.TextInputEditMenuTitle);
        IPartialTextInputComponent? descriptionComponent = textInputs.FirstOrDefault(c => c.CustomID == RoleComponentKeys.TextInputEditMenuDescription);

        if (titleComponent is null)
            return Result.FromError(new GenericCommandError());

        if (!titleComponent.Value.IsDefined(out titleText!))
            return Result.FromError(new GenericCommandError("We don't know how you managed it, but you need to submit a non-empty title!"));

        // The description is optional
        if (descriptionComponent is not null)
            descriptionComponent.Value.IsDefined(out descriptionText);

        return Result.FromSuccess();
    }
}
