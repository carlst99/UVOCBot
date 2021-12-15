﻿using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
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

internal sealed class ToggleRoleComponentResponder : IComponentResponder
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly DiscordContext _dbContext;
    private readonly InteractionContext _context;
    private readonly FeedbackService _feedbackService;

    public ToggleRoleComponentResponder
    (
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi,
        DiscordContext dbContext,
        InteractionContext context,
        FeedbackService feedbackService
    )
    {
        _channelApi = channelApi;
        _guildApi = guildApi;
        _dbContext = dbContext;
        _context = context;
        _feedbackService = feedbackService;
    }

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

        if (!menu.Roles.Any(r => r.RoleId == roleID.Value.Value))
            return new GenericCommandError(); // If this happens something sus is going on. Give no more info.

        string addedRoles = string.Empty;
        string removedRoles = string.Empty;

        bool shouldRemove = member.Roles.Contains(roleID.Value);
        Result roleManipulationResult;

        if (shouldRemove)
        {
            roleManipulationResult = await _guildApi.RemoveGuildMemberRoleAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                roleID.Value,
                "User self-removed via role menu",
                ct
            ).ConfigureAwait(false);
        }
        else
        {
            roleManipulationResult = await _guildApi.AddGuildMemberRoleAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                roleID.Value,
                "User self-added via role menu",
                ct
            ).ConfigureAwait(false);
        }

        string response = $"Sweet! You've been {(shouldRemove ? "relieved of" : "given")} the {Formatter.RoleMention(roleID.Value)} role.";

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

        GuildRoleMenu? menu = GetGuildRoleMenu(menuID.Value.Value);
        if (menu is null)
            return Result.FromError(new GenericCommandError());

        _dbContext.Remove(menu);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        Result deleteMenuResult = await _channelApi.DeleteMessageAsync
        (
            DiscordSnowflake.New(menu.ChannelId),
            DiscordSnowflake.New(menu.MessageId),
            "Role menu deletion requested by " + _context.User.Username,
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