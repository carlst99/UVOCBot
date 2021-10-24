using Remora.Results;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace UVOCBot.Plugins.Music.Abstractions.Services
{
    /// <summary>
    /// Represents an interface for retrieving information and audio streams from YouTube videos.
    /// </summary>
    public interface IYouTubeService
    {
        /// <summary>
        /// Gets information about a video.
        /// </summary>
        /// <param name="query">The queried video. Can be either a URL to the video, or a search term.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
        /// <returns>A result representing the outcome of the operation, and containing the video information if successful.</returns>
        Task<Result<Video>> GetVideoInfoAsync(string query, CancellationToken ct = default);

        /// <summary>
        /// Gets a valid audio stream for a video.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
        /// <returns>A result representing the outcome of the operation, and containing the audio stream if successful.</returns>
        Task<Result<Stream>> GetStreamAsync(Video video, CancellationToken ct = default);
    }
}
