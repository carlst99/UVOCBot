using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using UVOCBot.Core.Dto;

namespace UVOCBot.Core.Model;

public sealed class TwitterUser
{
    /// <summary>
    /// The twitter ID of this user
    /// </summary>
    [Key]
    public long UserId { get; set; }

    /// <summary>
    /// The last tweet that was relayed from this user
    /// </summary>
    public long? LastRelayedTweetId { get; set; }

    /// <summary>
    /// Guilds that are relaying tweets from this user
    /// </summary>
    public ICollection<GuildTwitterSettings> Guilds { get; set; } = new List<GuildTwitterSettings>();

    public TwitterUser() { }

    public TwitterUser(long id)
    {
        UserId = id;
        LastRelayedTweetId = null;
    }

    public TwitterUserDto ToDto()
        => new()
        {
            UserId = UserId,
            LastRelayedTweetId = LastRelayedTweetId,
            Guilds = Guilds.Select(g => g.GuildId).ToList()
        };

    public static async Task<TwitterUser> FromDto(TwitterUserDto dto, DiscordContext context)
    {
        TwitterUser user = new()
        {
            UserId = dto.UserId,
            LastRelayedTweetId = dto.LastRelayedTweetId
        };

        foreach (ulong id in dto.Guilds)
        {
            GuildTwitterSettings? tSettings = await context.FindAsync<GuildTwitterSettings>(id).ConfigureAwait(false);
            if (tSettings is not null)
                user.Guilds.Add(tSettings);
        }

        return user;
    }

    public override bool Equals(object? obj)
        => obj is TwitterUser user
        && user.UserId.Equals(UserId);

    public override int GetHashCode() => UserId.GetHashCode();
}
