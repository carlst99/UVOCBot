using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Core;
using Remora.Discord.Voice;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Plugins.Music.Abstractions.Services;
using UVOCBot.Plugins.Music.Errors;
using UVOCBot.Plugins.Music.Objects;
using YoutubeExplode.Videos;

namespace UVOCBot.Plugins.Music.Commands
{
    [RequireContext(ChannelContext.Guild)]
    public sealed class MusicCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly DiscordVoiceClientFactory _voiceClientFactory;
        private readonly IVoiceStateCacheService _voiceStateCache;
        private readonly IYouTubeService _youTubeService;
        private readonly IMusicService _musicService;
        private readonly FeedbackService _feedbackService;

        public MusicCommands
        (
            ICommandContext context,
            DiscordVoiceClientFactory voiceClientFactory,
            IVoiceStateCacheService voiceStateCache,
            IYouTubeService youTubeService,
            IMusicService musicService,
            FeedbackService feedbackService
        )
        {
            _context = context;
            _voiceClientFactory = voiceClientFactory;
            _voiceStateCache = voiceStateCache;
            _youTubeService = youTubeService;
            _musicService = musicService;
            _feedbackService = feedbackService;
        }

        [Command("play")]
        [Description("Plays audio from a YouTube video in the channel you are currently connected to.")]
        public async Task<Result> PlayCommandAsync
        (
            [Description("The URL/title of the YouTube video to play.")] string query
        )
        {
            Optional<IVoiceState> userVoiceState = _voiceStateCache.GetUserVoiceState(_context.User.ID);
            if (!userVoiceState.IsDefined() || !userVoiceState.Value.ChannelID.HasValue)
                return new GenericCommandError("You must be in a voice channel to use this command.");

            if (!userVoiceState.Value.GuildID.HasValue || userVoiceState.Value.GuildID.Value != _context.GuildID.Value)
                return new GenericCommandError("The voice channel you are in must be within the same guild that you called this command in.");

            if (_musicService.GetCurrentlyPlaying(_context.GuildID.Value) is not null)
            {
                return new GenericCommandError
                (
                    "Music is already being played in another channel." +
                    "You cannot queue music up in your channel while this is happening."
                );
            }

            Result checkPermissions = await _musicService.HasMusicPermissionsAsync
            (
                userVoiceState.Value.ChannelID.Value,
                CancellationToken
            ).ConfigureAwait(false);

            if (!checkPermissions.IsSuccess)
                return checkPermissions;

            Result<Video> getVideo = await _youTubeService.GetVideoInfoAsync(query, CancellationToken).ConfigureAwait(false);
            if (!getVideo.IsSuccess)
            {
                if (getVideo.Error is YouTubeUserError yue)
                    return new GenericCommandError(yue.Message);

                return Result.FromError(getVideo);
            }

            _musicService.Enqueue
            (
                new MusicRequest
                (
                    _context.GuildID.Value,
                    userVoiceState.Value.ChannelID.Value,
                    _context.ChannelID,
                    getVideo.Entity
                )
            );

            Embed responseEmbed = new
            (
                $"Queued {Formatter.Bold(getVideo.Entity.Title)} ({getVideo.Entity.Duration:mm\\:ss})",
                Description: getVideo.Entity.Author.Title + "\n" + getVideo.Entity.Url,
                Thumbnail: new EmbedThumbnail(getVideo.Entity.Thumbnails[0].Url),
                Colour: DiscordConstants.DEFAULT_EMBED_COLOUR
            );

            var sendResult = await _feedbackService.SendContextualEmbedAsync(responseEmbed, ct: CancellationToken).ConfigureAwait(false);

            return !sendResult.IsSuccess
                ? Result.FromError(sendResult.Error!)
                : Result.FromSuccess();
        }

        [Command("skip")]
        [Description("Skips songs in the play queue.")]
        public async Task<Result> SkipCommandAsync(int amount = 1)
        {
            await _musicService.SkipAsync(_context.GuildID.Value, amount, CancellationToken).ConfigureAwait(false);

            var sendResult = await _feedbackService.SendContextualSuccessAsync
            (
                $"Skipped {amount} songs.",
                ct: CancellationToken
            ).ConfigureAwait(false);

            return !sendResult.IsSuccess
                ? Result.FromError(sendResult)
                : Result.FromSuccess();
        }

        [Command("clear")]
        [Description("Clears the play queue.")]
        public async Task<Result> ClearQueueCommandAsync()
        {
            _musicService.ClearQueue(_context.GuildID.Value);

            var sendResult = await _feedbackService.SendContextualSuccessAsync
            (
                "The play queue has been cleared.",
                ct: CancellationToken
            ).ConfigureAwait(false);

            return !sendResult.IsSuccess
                ? Result.FromError(sendResult)
                : Result.FromSuccess();
        }

        [Command("leave")]
        [Description("Leaves the voice channel.")]
        public async Task<Result> LeaveCommandAsync()
        {
            _musicService.ClearQueue(_context.GuildID.Value);

            Result disconnectResult = await _musicService.ForceDisconnectAsync(_context.GuildID.Value, CancellationToken).ConfigureAwait(false);
            if (!disconnectResult.IsSuccess)
                return new GenericCommandError("Failed to leave the voice channel!");

            var sendResult = await _feedbackService.SendContextualSuccessAsync
            (
                "Goodbye!",
                ct: CancellationToken
            ).ConfigureAwait(false);

            return !sendResult.IsSuccess
                ? Result.FromError(sendResult)
                : Result.FromSuccess();
        }

        [Command("show-queue")]
        [Description("Shows the current music queue.")]
        [Ephemeral]
        public async Task<Result> ShowQueueCommandAsync()
        {
            var queue = _musicService.GetQueue(_context.GuildID.Value);
            string description = string.Empty;

            for (int i = 1; i <= queue.Count; i++)
                description += $"{i}. {queue[i].Video.Title}\n";

            if (queue.Count == 0)
                description = "The queue is empty!";

            var sendResult = await _feedbackService.SendContextualInfoAsync(description, ct: CancellationToken).ConfigureAwait(false);

            return !sendResult.IsSuccess
                ? Result.FromError(sendResult)
                : Result.FromSuccess();
        }
    }
}
