using Remora.Discord.Core;
using Remora.Results;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UVOCBot.Plugins.Music.Abstractions.Services
{
    /// <summary>
    /// Represents an interface for playing audio to a guild.
    /// </summary>
    public interface IMusicService
    {
        /// <summary>
        /// Converts an audio stream to PCM-16, using ffmpeg.
        /// </summary>
        /// <param name="input">The audio stream to convert.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the underlying ffmpeg conversion.</param>
        /// <returns>The stream of audio. It will not be complete when this method returns.</returns>
        Stream ConvertToPcmInBackground(Stream input, CancellationToken ct = default);

        /// <summary>
        /// Plays an audio stream.
        /// </summary>
        /// <param name="guildID">The guild to connect to.</param>
        /// <param name="channelID">The channel to play the audio in.</param>
        /// <param name="pcmAudio">The stream of PCM-16 audio to play.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
        /// <returns>A result representing the outcome of the operation.</returns>
        Task<Result> PlayAsync(Snowflake guildID, Snowflake channelID, Stream pcmAudio, CancellationToken ct = default);
    }
}
