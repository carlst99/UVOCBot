using Remora.Discord.Voice.Util;
using Remora.Results;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Music.Abstractions.Services;
using UVOCBot.Plugins.Music.Errors;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace UVOCBot.Plugins.Music.MusicService
{
    /// <inheritdoc cref="IYouTubeService"/>
    public class YouTubeService : IYouTubeService
    {
        private readonly YoutubeClient _youtubeClient;

        public YouTubeService(YoutubeClient youtubeClient)
        {
            _youtubeClient = youtubeClient;
        }

        /// <inheritdoc />
        public async Task<Result<Video>> GetVideoInfoAsync(string query, CancellationToken ct = default)
        {
            try
            {
                Video? video = null;

                if (Uri.TryCreate(query, UriKind.Absolute, out Uri? parsedUri))
                {
                    // TODO: Verify parsed URI

                    try
                    {
                        video = await _youtubeClient.Videos.GetAsync(query, ct).ConfigureAwait(false);
                    }
                    catch (ArgumentException)
                    {
                        return new YouTubeUserError("That's an invalid video URL.");
                    }
                }
                else
                {
                    // Naively take the first result. Could be improved?
                    await foreach (VideoSearchResult searchResult in _youtubeClient.Search.GetVideosAsync(query, ct))
                    {
                        video = await _youtubeClient.Videos.GetAsync(searchResult.Url, ct).ConfigureAwait(false);
                        break;
                    }

                    if (video is null)
                        return new YouTubeUserError("No videos matched your query.");
                }

                return video;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <inheritdoc />
        public async Task<Result<Stream>> GetStreamAsync(Video video, CancellationToken ct = default)
        {
            try
            {
                StreamManifest manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id, ct).ConfigureAwait(false);

                IAudioStreamInfo? audioStreamDetails = manifest
                    .GetAudioStreams()
                    .FirstOrDefault(s => s.Bitrate.BitsPerSecond <= VoiceConstants.DiscordMaxBitrate);

                if (audioStreamDetails is null)
                    return new YouTubeUserError("Failed to retrieve a valid audio stream for that video. Are you sure it has sound?");

                return await _youtubeClient.Videos.Streams.GetAsync(audioStreamDetails, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
