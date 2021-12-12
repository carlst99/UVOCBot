using Microsoft.EntityFrameworkCore;
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

namespace UVOCBot.Plugins.Roles.Responders;

internal sealed class ToggleRoleComponentResponder : IComponentResponder
{
    private readonly DiscordContext _dbContext;
    private readonly InteractionContext _context;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly FeedbackService _feedbackService;

    public ToggleRoleComponentResponder
    (
        DiscordContext dbContext,
        InteractionContext context,
        IDiscordRestGuildAPI guildApi,
        FeedbackService feedbackService
    )
    {
        _dbContext = dbContext;
        _context = context;
        _guildApi = guildApi;
        _feedbackService = feedbackService;
    }

    public async Task<Result> RespondAsync(string key, string? dataFragment, CancellationToken ct = default)
    {
        if (!_context.GuildID.HasValue || !_context.Member.HasValue || _context.Member.Value is null || !_context.Data.Values.HasValue || dataFragment is null)
            return Result.FromSuccess();

        ulong messageId = ulong.Parse(dataFragment);

        GuildRoleMenu? menu = GetGuildRoleMenu(messageId);
        if (menu is null)
        {
            IResult sendErrorResult = await _feedbackService.SendContextualErrorAsync
            (
                DiscordConstants.GENERIC_ERROR_MESSAGE,
                ct: ct
            ).ConfigureAwait(false);

            return sendErrorResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendErrorResult.Error!);
        }

        string addedRoles = string.Empty;
        string removedRoles = string.Empty;

        foreach (string option in _context.Data.Values.Value)
        {
            Snowflake roleId = new(ulong.Parse(option), Remora.Discord.API.Constants.DiscordEpoch);

            if (_context.Member.Value.Roles.Contains(roleId))
            {
                Result removeRoleResult = await _guildApi.RemoveGuildMemberRoleAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    roleId,
                    "User self-removed via role menu",
                    ct
                ).ConfigureAwait(false);

                if (removeRoleResult.IsSuccess)
                    removedRoles += Formatter.RoleMention(roleId) + " ";
            }
            else
            {
                Result addRoleResult = await _guildApi.AddGuildMemberRoleAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    roleId,
                    "User self-added via role menu",
                    ct
                ).ConfigureAwait(false);

                if (addRoleResult.IsSuccess)
                    addedRoles += Formatter.RoleMention(roleId) + " ";
            }
        }

        string response = string.Empty;
        if (addedRoles != string.Empty)
        {
            response += "Sweet! I've given you the following roles:" +
                "\n" + addedRoles + "\n\n";
        }

        if (removedRoles != string.Empty)
        {
            response += "I removed the following roles, because you already had them:" +
                "\n" + removedRoles;
        }

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

    private GuildRoleMenu? GetGuildRoleMenu(ulong messageId)
        => _dbContext.RoleMenus.Include(r => r.Roles).FirstOrDefault(r => r.GuildId == _context.GuildID.Value.Value && r.MessageId == messageId);
}
