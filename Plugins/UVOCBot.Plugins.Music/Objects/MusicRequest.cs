using Remora.Discord.Core;
using YoutubeExplode.Videos;

namespace UVOCBot.Plugins.Music.Objects;

/// <summary>
/// Represents a request to play music in a channel.
/// </summary>
/// <param name="GuildID">The guild the request is for.</param>
/// <param name="ChannelID">The channel to connect to.</param>
/// <param name="ContextChannelID">The text channel the request was made in, to relay status to.</param>
/// <param name="Video">The video to play.</param>
public record MusicRequest
(
    Snowflake GuildID,
    Snowflake ChannelID,
    Snowflake ContextChannelID,
    Video Video
);
