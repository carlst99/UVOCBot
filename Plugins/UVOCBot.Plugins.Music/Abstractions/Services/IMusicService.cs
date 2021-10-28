using Remora.Discord.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Music.Objects;

namespace UVOCBot.Plugins.Music.Abstractions.Services
{
    /// <summary>
    /// Represents an interface for playing audio to a guild.
    /// </summary>
    public interface IMusicService
    {
        /// <summary>
        /// Runs the service. This method does not return until cancelled.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the operation.</param>
        /// <returns>A result representing the outcome of the operation.</returns>
        Task<Result> RunAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks to see if the bot has the permissions it requires to play audio in the given channel.
        /// </summary>
        /// <param name="channelID">The channel.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
        /// <returns>A result representing the outcome of the operation.</returns>
        Task<Result> HasMusicPermissionsAsync(Snowflake channelID, CancellationToken ct = default);

        /// <summary>
        /// Enqueues a music request.
        /// </summary>
        /// <param name="request"></param>
        void Enqueue(MusicRequest request);

        /// <summary>
        /// Skips a number of tracks in a particular guild's queue.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <param name="amount">The number of tracks to skip.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
        Task SkipAsync(Snowflake guildID, int amount = 1, CancellationToken ct = default);

        /// <summary>
        /// Clears the play queue for a particular guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        void ClearQueue(Snowflake guildID);

        /// <summary>
        /// Gets the music queue for a particular guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <returns>The items in the queue.</returns>
        IReadOnlyList<MusicRequest> GetQueue(Snowflake guildID);

        /// <summary>
        /// Gets the currently playing music for a particular guild.
        /// </summary>
        /// <param name="guildID">The ID of the guild.</param>
        /// <returns>The currently playing track, or <c>null</c> if there is none.</returns>
        MusicRequest? GetCurrentlyPlaying(Snowflake guildID);

        /// <summary>
        /// Forces disconnecting from a voice channel.
        /// </summary>
        /// <param name="guildID">The guild to disconnect from.</param>
        /// <returns>A result representing the outcome of the operation.</returns>
        Task<Result> ForceDisconnectAsync(Snowflake guildID, CancellationToken ct = default);
    }
}
