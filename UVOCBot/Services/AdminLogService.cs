using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Commands;
using UVOCBot.Core;
using UVOCBot.Core.Model;
using UVOCBot.Model;
using UVOCBot.Services.Abstractions;

namespace UVOCBot.Services
{
    public class AdminLogService : IAdminLogService
    {
        private readonly DiscordContext _dbContext;
        private readonly IDiscordRestChannelAPI _channelApi;

        public AdminLogService(DiscordContext dbContext, IDiscordRestChannelAPI channelApi)
        {
            _dbContext = dbContext;
            _channelApi = channelApi;
        }

        public async Task<Result> LogMemberJoin(IGuildMemberAdd member, CancellationToken ct = default)
        {
            if (!member.User.HasValue || member.User.Value is null)
                return Result.FromSuccess();
            IUser user = member.User.Value;

            Result<Snowflake> canLogToChannel = await CheckCanLog(member.GuildID, AdminLogTypes.MemberLeave, ct).ConfigureAwait(false);
            if (!canLogToChannel.IsSuccess)
                return Result.FromSuccess();

            Embed e = new()
            {
                Title = Formatter.Bold("A member has joined: ") + user.Username,
                Colour = Color.Green,
                Timestamp = DateTimeOffset.UtcNow
            };

            Result<Uri> userAvatar = CDN.GetUserAvatarUrl(user);
            if (userAvatar.IsSuccess)
                e = e with { Thumbnail = new EmbedThumbnail(userAvatar.Entity.AbsoluteUri) };

            _ = _channelApi.CreateMessageAsync(canLogToChannel.Entity, embeds: new List<IEmbed> { e }, ct: ct);

            return Result.FromSuccess();
        }

        public async Task<Result> LogMemberLeave(IGuildMemberRemove user, CancellationToken ct = default)
        {
            Result<Snowflake> canLogToChannel = await CheckCanLog(user.GuildID, AdminLogTypes.MemberLeave, ct).ConfigureAwait(false);
            if (!canLogToChannel.IsSuccess)
                return Result.FromSuccess();

            Embed e = new()
            {
                Title = Formatter.Bold("A member has left: ") + user.User.Username,
                Colour = Color.Red,
                Timestamp = DateTimeOffset.UtcNow
            };

            Result<Uri> userAvatar = CDN.GetUserAvatarUrl(user.User);
            if (userAvatar.IsSuccess)
                e = e with { Thumbnail = new EmbedThumbnail(userAvatar.Entity.AbsoluteUri) };

            _ = _channelApi.CreateMessageAsync(canLogToChannel.Entity, embeds: new List<IEmbed> { e }, ct: ct);

            return Result.FromSuccess();
        }

        private async Task<Result<Snowflake>> CheckCanLog(Snowflake guildId, AdminLogTypes logType, CancellationToken ct = default)
        {
            GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(guildId.Value, ct).ConfigureAwait(false);

            if (settings.LoggingChannelId is null)
                return new Exception("No logging channel has been set.");

            if ((settings.LogTypes & (ulong)logType) != 0 || logType == AdminLogTypes.None) // Allow none through for non log-event logging
                return new Snowflake(settings.LoggingChannelId.Value);
            else
                return new Exception("That logging type hasn't been enabled for the given guild.");
        }
    }
}
