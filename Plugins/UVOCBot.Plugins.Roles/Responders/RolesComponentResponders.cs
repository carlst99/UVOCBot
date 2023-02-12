using Microsoft.EntityFrameworkCore;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using UVOCBot.Discord.Core.Commands;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Components;
using UVOCBot.Discord.Core.Errors;

namespace UVOCBot.Plugins.Roles.Responders;

internal sealed class RolesComponentResponders : IComponentResponder
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly DiscordContext _dbContext;
    private readonly IInteraction _context;
    private readonly FeedbackService _feedbackService;

    public RolesComponentResponders
    (
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        DiscordContext dbContext,
        IInteractionContext context,
        FeedbackService feedbackService
    )
    {
        _channelApi = channelApi;
        _guildApi = guildApi;
        _dbContext = dbContext;
        _context = context.Interaction;
        _feedbackService = feedbackService;
    }

    public Result<Attribute[]> GetResponseAttributes(string key)
        => key switch
        {
            RoleComponentKeys.ToggleRole => Result<Attribute[]>.FromSuccess(new Attribute[] { new EphemeralAttribute() }),
            _ => Array.Empty<Attribute>()
        };

    public async Task<IResult> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
        => key switch
        {
            RoleComponentKeys.ConfirmDeletion => await DeleteMenuAsync(dataFragment, ct).ConfigureAwait(false),
            RoleComponentKeys.ToggleRole => await ToggleRoleAsync(dataFragment, ct).ConfigureAwait(false),
            _ => Result.FromError(new GenericCommandError())
        };

    private async Task<Result> ToggleRoleAsync(string? dataFragment, CancellationToken ct = default)
    {
        if (!_context.GuildID.IsDefined(out Snowflake guildID))
            return Result.FromSuccess();

        if (!_context.Member.IsDefined(out IGuildMember? member))
            return Result.FromSuccess();

        if (!_context.Message.IsDefined(out IMessage? message))
            return Result.FromSuccess();

        if (!_context.TryGetUser(out IUser? user))
            return Result.FromSuccess();

        if (dataFragment is null)
            return Result.FromSuccess();

        if (!DiscordSnowflake.TryParse(dataFragment, out Snowflake? roleID))
            return Result.FromSuccess();

        GuildRoleMenu? menu = GetGuildRoleMenu(message.ID.Value);
        if (menu is null)
        {
            IResult sendDeletedResponseResult = await _feedbackService.SendContextualErrorAsync
            (
                "This role menu has been deleted by an administrator. You can no longer use it.",
                ct: ct
            ).ConfigureAwait(false);

            return !sendDeletedResponseResult.IsSuccess
                ? Result.FromError(sendDeletedResponseResult.Error!)
                : Result.FromSuccess();
        }

        if (menu.Roles.All(r => r.RoleId != roleID.Value.Value))
            return new GenericCommandError(); // If this happens something sus is going on. Give no more info.

        bool shouldRemove = member.Roles.Contains(roleID.Value);
        Result roleManipulationResult;

        if (shouldRemove)
        {
            roleManipulationResult = await _guildApi.RemoveGuildMemberRoleAsync
            (
                guildID,
                user.ID,
                roleID.Value,
                "User self-removed via role menu",
                ct
            ).ConfigureAwait(false);
        }
        else
        {
            roleManipulationResult = await _guildApi.AddGuildMemberRoleAsync
            (
                guildID,
                user.ID,
                roleID.Value,
                "User self-added via role menu",
                ct
            ).ConfigureAwait(false);
        }

        if (!roleManipulationResult.IsSuccess)
            return roleManipulationResult;

        string response = shouldRemove
            ? $"The {Formatter.RoleMention(roleID.Value)} role has been removed."
            : $"Sweet! You've been given the {Formatter.RoleMention(roleID.Value)} role.";

        IResult res = await _feedbackService.SendContextualSuccessAsync
        (
            response,
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions()),
            ct: ct
        ).ConfigureAwait(false);

        return res.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(res.Error!);
    }

    private async Task<IResult> DeleteMenuAsync(string? dataFragment, CancellationToken ct = default)
    {
        if (dataFragment is null)
            return Result.FromError(new GenericCommandError());

        if (!DiscordSnowflake.TryParse(dataFragment, out Snowflake? menuID))
            return Result.FromSuccess();

        if (!_context.TryGetUser(out IUser? user))
            return Result.FromSuccess();

        GuildRoleMenu? menu = GetGuildRoleMenu(menuID.Value.Value);
        if (menu is null)
            return Result.FromError(new GenericCommandError());

        _dbContext.Remove(menu);
        int deletedCount = await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        if (deletedCount < 1)
            return Result.FromError(new GenericCommandError());

        Result deleteMenuResult = await _channelApi.DeleteMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            "Role menu deletion requested by " + user.Username,
            ct
        ).ConfigureAwait(false);

        if (!deleteMenuResult.IsSuccess)
        {
            return await _feedbackService.SendContextualWarningAsync
            (
                "The role menu has successfully been deleted but I couldn't remove the corresponding message. Please delete that now, as well.",
                ct: ct
            ).ConfigureAwait(false);
        }

        return await _feedbackService.SendContextualSuccessAsync
        (
            "The role menu has been successfully removed.",
            ct: ct
        ).ConfigureAwait(false);
    }

    private GuildRoleMenu? GetGuildRoleMenu(ulong messageId)
        => _dbContext.RoleMenus.Include(r => r.Roles).FirstOrDefault(r => r.GuildId == _context.GuildID.Value.Value && r.MessageId == messageId);
}
