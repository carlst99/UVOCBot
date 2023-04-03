using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Abstractions.Services;
using UVOCBot.Core;
using UVOCBot.Core.Extensions;
using UVOCBot.Core.Model;
using UVOCBot.Discord.Core;
using UVOCBot.Model;

namespace UVOCBot.Services;

public class AdminLogService : IAdminLogService
{
    private readonly DiscordContext _dbContext;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;

    public AdminLogService
    (
        DiscordContext dbContext,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestGuildAPI guildApi
    )
    {
        _dbContext = dbContext;
        _channelApi = channelApi;
        _guildApi = guildApi;
    }

    public async Task<Result> LogMemberJoin(IGuildMemberAdd member, CancellationToken ct = default)
    {
        if (!member.User.IsDefined(out IUser? user))
            return Result.FromSuccess();

        Result<Snowflake> canLogToChannel = await CheckCanLog(member.GuildID, AdminLogTypes.MemberJoin, ct);
        if (!canLogToChannel.IsSuccess)
            return Result.FromSuccess();

        Embed e = new()
        {
            Title = Formatter.Bold("A member has joined: ") + Formatter.UserMention(user),
            Colour = Color.Green,
            Timestamp = DateTimeOffset.UtcNow
        };

        Result<Uri> userAvatar = CDN.GetUserAvatarUrl(user);
        if (userAvatar.IsSuccess)
            e = e with { Thumbnail = new EmbedThumbnail(userAvatar.Entity.AbsoluteUri, Height: 64, Width: 64) };

        _ = _channelApi.CreateMessageAsync(canLogToChannel.Entity, embeds: new List<IEmbed> { e }, ct: ct);

        return Result.FromSuccess();
    }

    public async Task<Result> LogMemberLeave(IGuildMemberRemove user, CancellationToken ct = default)
    {
        Result<Snowflake> canLogToChannel = await CheckCanLog(user.GuildID, AdminLogTypes.MemberLeave, ct);
        if (!canLogToChannel.IsSuccess)
            return Result.FromSuccess();

        List<IEmbedField> embedFields = new();

        EmbedField infoField = new
        (
            "Information",
            Formatter.UserMention(user.User)
            + $"\nUsername: {user.User.Username}#{user.User.Discriminator}"
            + $"\nID: {user.User.ID}"
        );

        // This will pull from cache. If not in cache, we might be very lucky and get a result from the API
        Result<IGuildMember> memberResult = await _guildApi.GetGuildMemberAsync(user.GuildID, user.User.ID, ct);
        if (memberResult.IsDefined(out IGuildMember? member))
        {
            infoField = infoField with { Value = infoField.Value + $"\nLast Nickname: {member.Nickname}" };

            EmbedField joinedAtField = new
            (
                "Joined At",
                Formatter.Timestamp(member.JoinedAt, TimestampStyle.LongDateTime)
                + $"\n({Formatter.Timestamp(member.JoinedAt, TimestampStyle.RelativeTime)})"
            );
            embedFields.Add(joinedAtField);

            EmbedField rolesField = new
            (
                "Roles",
                string.Join(", ", member.Roles.Select(Formatter.RoleMention))
            );
            embedFields.Add(rolesField);
        }

        embedFields.Add(infoField);

        Embed e = new()
        {
            Title = user.User.Username + " has left",
            Colour = Color.Red,
            Timestamp = DateTimeOffset.UtcNow,
            Fields = embedFields
        };

        Result<Uri> userAvatar = CDN.GetUserAvatarUrl(user.User);
        if (userAvatar.IsSuccess)
            e = e with { Thumbnail = new EmbedThumbnail(userAvatar.Entity.AbsoluteUri, Height: 64, Width: 64) };

        _ = _channelApi.CreateMessageAsync(canLogToChannel.Entity, embeds: new List<IEmbed> { e }, ct: ct);

        return Result.FromSuccess();
    }

    private async Task<Result<Snowflake>> CheckCanLog(Snowflake guildId, AdminLogTypes logType, CancellationToken ct = default)
    {
        GuildAdminSettings settings = await _dbContext.FindOrDefaultAsync<GuildAdminSettings>(guildId.Value, ct).ConfigureAwait(false);

        if (settings.LoggingChannelId is null)
            return new Exception("No logging channel has been set.");

        if ((settings.LogTypes & (ulong)logType) != 0 || logType == AdminLogTypes.None) // Allow none through for non log-event logging
            return new Snowflake(settings.LoggingChannelId.Value, Constants.DiscordEpoch);
        else
            return new Exception("That logging type hasn't been enabled for the given guild.");
    }
}
