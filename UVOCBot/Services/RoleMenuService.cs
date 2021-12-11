using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services;

public class RoleMenuService : IRoleMenuService
{
    private readonly DiscordContext _dbContext;
    private readonly InteractionContext _context;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IReplyService _replyService;

    public RoleMenuService(
        DiscordContext dbContext,
        InteractionContext context,
        IDiscordRestGuildAPI guildApi,
        IReplyService replyService)
    {
        _dbContext = dbContext;
        _context = context;
        _guildApi = guildApi;
        _replyService = replyService;
    }

    public async Task<Result> ToggleRolesAsync(CancellationToken ct = default)
    {
        if (!_context.GuildID.HasValue || !_context.Member.HasValue || _context.Member.Value is null || !_context.Data.Values.HasValue)
            return Result.FromSuccess();

        ComponentIdFormatter.Parse(_context.Data.CustomID.Value, out _, out string messageIdString);
        ulong messageId = ulong.Parse(messageIdString);

        GuildRoleMenu? menu = GetGuildRoleMenu(messageId);
        if (menu is null)
        {
            Result<IMessage> sendErrorResult = await _replyService.RespondWithErrorAsync(ct).ConfigureAwait(false);
            return sendErrorResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendErrorResult);
        }

        string addedRoles = string.Empty;
        string removedRoles = string.Empty;

        foreach (string option in _context.Data.Values.Value)
        {
            Snowflake roleId = new(ulong.Parse(option), Remora.Discord.API.Constants.DiscordEpoch);

            if (_context.Member.Value.Roles.Contains(roleId))
            {
                Result removeRoleResult = await _guildApi.RemoveGuildMemberRoleAsync(_context.GuildID.Value, _context.User.ID, roleId, "User self-removed via role menu", ct).ConfigureAwait(false);
                if (removeRoleResult.IsSuccess)
                    removedRoles += Formatter.RoleMention(roleId) + " ";
            }
            else
            {
                Result addRoleResult = await _guildApi.AddGuildMemberRoleAsync(_context.GuildID.Value, _context.User.ID, roleId, "User self-added via role menu", ct).ConfigureAwait(false);
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

        Result<IMessage> res = await _replyService.RespondWithSuccessAsync(response, ct, new AllowedMentions()).ConfigureAwait(false);
        return res.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(res);
    }

    public async Task<Result> ConfirmRemoveRolesAsync(CancellationToken ct = default)
    {
        return Result.FromSuccess();
    }

    private GuildRoleMenu? GetGuildRoleMenu(ulong messageId)
        => _dbContext.RoleMenus.Include(r => r.Roles).FirstOrDefault(r => r.GuildId == _context.GuildID.Value.Value && r.MessageId == messageId);
}
