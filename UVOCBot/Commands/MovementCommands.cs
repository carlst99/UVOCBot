using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Commands
{
    [RequireContext(ChannelContext.Guild)]
    [RequireGuildPermission(DiscordPermission.MoveMembers)]
    public class MovementCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordContext _dbContext;
        private readonly IReplyService _replyService;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private readonly IVoiceStateCacheService _voiceStateCache;

        public MovementCommands(ICommandContext context, DiscordContext dbContext, IReplyService replyService, IDiscordRestGuildAPI guildAPI, IVoiceStateCacheService voiceStateCache)
        {
            _context = context;
            _dbContext = dbContext;
            _replyService = replyService;
            _guildAPI = guildAPI;
            _voiceStateCache = voiceStateCache;
        }

        [Command("move")]
        [Description("Moves a group from one voice channel to another")]
        public async Task<IResult> MoveCommandAsync
        (
            [Description("The voice channel to move people to")]
            [ChannelTypes(ChannelType.GuildVoice, ChannelType.GuildStageVoice)]
            IChannel moveTo,

            [Description("An optional voice channel to move people from")]
            [ChannelTypes(ChannelType.GuildVoice, ChannelType.GuildStageVoice)]
            IChannel? moveFrom = null
        )
        {
            if (moveTo.Type != ChannelType.GuildVoice)
            {
                await _replyService.RespondWithUserErrorAsync($"Please specify a valid { Formatter.Bold("voice") } channel to move to.", ct: CancellationToken).ConfigureAwait(false);
                return Result.FromSuccess();
            }

            Result<Snowflake> moveFromID = await GetMoveFromChannelIdAsync(moveFrom).ConfigureAwait(false);
            if (!moveFromID.IsSuccess)
                return Result.FromSuccess();

            Optional<IReadOnlyList<IVoiceState>> voiceStates = _voiceStateCache.GetChannelVoiceStates(moveFromID.Entity);
            if (!voiceStates.HasValue)
                return await _replyService.RespondWithUserErrorAsync("There are no members to move", ct: CancellationToken).ConfigureAwait(false);

            foreach (IVoiceState voiceState in voiceStates.Value)
                await _guildAPI.ModifyGuildMemberAsync(_context.GuildID.Value, voiceState.UserID, channelID: moveTo.ID, ct: CancellationToken).ConfigureAwait(false);

            return await _replyService.RespondWithSuccessAsync(Formatter.Emoji("white_check_mark"), ct: CancellationToken).ConfigureAwait(false);
        }

        [Command("move-group")]
        [Description("Moves a group from one voice channel to another")]
        public async Task<IResult> MoveGroupCommandAsync
        (
            [Description("The member group to move. See the group commands for more info")]
            string groupName,

            [Description("The voice channel to move people to")]
            [ChannelTypes(ChannelType.GuildVoice, ChannelType.GuildStageVoice)]
            IChannel moveTo,

            [Description("An optional voice channel to move people from")]
            [ChannelTypes(ChannelType.GuildVoice, ChannelType.GuildStageVoice)]
            IChannel? moveFrom = null
        )
        {
            if (moveTo.Type != ChannelType.GuildVoice)
            {
                await _replyService.RespondWithUserErrorAsync($"Please specify a valid {Formatter.Bold("voice")} channel to move to.", ct: CancellationToken).ConfigureAwait(false);
                return Result.FromSuccess();
            }

            Result<Snowflake> moveFromID = await GetMoveFromChannelIdAsync(moveFrom).ConfigureAwait(false);
            if (!moveFromID.IsSuccess)
                return Result.FromSuccess();

            Result<IReadOnlyList<Snowflake>> groupMembers = await GetGroupMembersAsync(groupName).ConfigureAwait(false);
            if (!groupMembers.IsSuccess)
                return Result.FromSuccess(); // Not our error if the group cannot be found

            Optional<IReadOnlyList<IVoiceState>> voiceStates = _voiceStateCache.GetChannelVoiceStates(moveFromID.Entity);
            if (!voiceStates.HasValue)
                return await _replyService.RespondWithUserErrorAsync("There are no members to move", ct: CancellationToken).ConfigureAwait(false);

            foreach (IVoiceState voiceState in voiceStates.Value)
            {
                if (groupMembers.Entity.Contains(voiceState.UserID))
                    await _guildAPI.ModifyGuildMemberAsync(_context.GuildID.Value, voiceState.UserID, channelID: moveTo.ID, ct: CancellationToken).ConfigureAwait(false);
            }

            return await _replyService.RespondWithSuccessAsync(Formatter.Emoji("white_check_mark"), ct: CancellationToken).ConfigureAwait(false);
        }

        private async Task<Result<Snowflake>> GetMoveFromChannelIdAsync(IChannel? moveFromChannel = null)
        {
            if (moveFromChannel is null)
            {
                Optional<IVoiceState> memberState = _voiceStateCache.GetUserVoiceState(_context.User.ID);
                if (!memberState.HasValue)
                {
                    await _replyService.RespondWithUserErrorAsync("You must be in a voice channel to omit the " + Formatter.InlineQuote("moveFrom") + "parameter.", ct: CancellationToken).ConfigureAwait(false);
                    return Result<Snowflake>.FromError(new InvalidOperationException());
                }

                return memberState.Value.ChannelID!.Value;
            }
            else
            {
                if (moveFromChannel.Type != ChannelType.GuildVoice)
                {
                    await _replyService.RespondWithUserErrorAsync($"Please specify a valid {Formatter.Bold("voice")} channel to move from.", ct: CancellationToken).ConfigureAwait(false);
                    return Result<Snowflake>.FromError(new InvalidOperationException());
                }

                return moveFromChannel.ID;
            }
        }

        private async Task<Result<IReadOnlyList<Snowflake>>> GetGroupMembersAsync(string groupName)
        {
            MemberGroup? group = await _dbContext.MemberGroups.FirstAsync(g => g.GuildId == _context.GuildID.Value.Value && g.GroupName == groupName, CancellationToken).ConfigureAwait(false);

            if (group is null)
            {
                await _replyService.RespondWithUserErrorAsync("A group with that name does not exist.", CancellationToken).ConfigureAwait(false);
                return new NotFoundError();
            }

            return group.UserIds.ConvertAll(i => new Snowflake(i)).AsReadOnly();
        }
    }
}
