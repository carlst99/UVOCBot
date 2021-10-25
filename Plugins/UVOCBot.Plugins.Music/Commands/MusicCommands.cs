using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Core;
using Remora.Results;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Commands.Conditions.Attributes;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Plugins.Music.Abstractions.Services;
using UVOCBot.Plugins.Music.Errors;
using YoutubeExplode.Videos;

namespace UVOCBot.Plugins.Music.Commands
{
    [RequireContext(ChannelContext.Guild)]
    public sealed class MusicCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly IVoiceStateCacheService _voiceStateCache;
        private readonly IYouTubeService _youTubeService;
        private readonly IMusicService _musicService;
        private readonly IPermissionChecksService _permissionChecksService;
        private readonly FeedbackService _feedbackService;

        public MusicCommands
        (
            ICommandContext context,
            IVoiceStateCacheService voiceStateCache,
            IYouTubeService youTubeService,
            IMusicService musicService,
            IPermissionChecksService permissionChecksService,
            FeedbackService feedbackService
        )
        {
            _context = context;
            _voiceStateCache = voiceStateCache;
            _youTubeService = youTubeService;
            _musicService = musicService;
            _permissionChecksService = permissionChecksService;
            _feedbackService = feedbackService;
        }

        [Command("play")]
        [Description("Plays audio from a YouTube video in the channel you are currently connected to.")]
        public async Task<Result> PlayCommandAsync
        (
            [Description("The URL/title of the YouTube video to play.")] string query
        )
        {
            try
            {
                Optional<IVoiceState> userVoiceState = _voiceStateCache.GetUserVoiceState(_context.User.ID);
                if (!userVoiceState.IsDefined() || !userVoiceState.Value.ChannelID.HasValue)
                    return new GenericCommandError("You must be in a voice channel to use this command.");

                if (!userVoiceState.Value.GuildID.HasValue || userVoiceState.Value.GuildID.Value != _context.GuildID.Value)
                    return new GenericCommandError("The voice channel you are in must be within the same guild that you called this command in.");

                Result<IDiscordPermissionSet> channelPermissions = await _permissionChecksService.GetPermissionsInChannel
                (
                    userVoiceState.Value.ChannelID.Value,
                    DiscordConstants.UserId,
                    CancellationToken
                ).ConfigureAwait(false);

                if (!channelPermissions.IsDefined())
                    return Result.FromError(channelPermissions);

                if (!channelPermissions.Entity.HasAdminOrPermission(DiscordPermission.Connect))
                    return new PermissionError(DiscordPermission.Connect, DiscordConstants.UserId, userVoiceState.Value.ChannelID.Value);

                if (!channelPermissions.Entity.HasAdminOrPermission(DiscordPermission.Speak))
                    return new PermissionError(DiscordPermission.Speak, DiscordConstants.UserId, userVoiceState.Value.ChannelID.Value);

                Result<Video> getVideo = await _youTubeService.GetVideoInfoAsync(query, CancellationToken).ConfigureAwait(false);
                if (!getVideo.IsSuccess)
                {
                    if (getVideo.Error is YouTubeUserError yue)
                        return new GenericCommandError(yue.Message);

                    return Result.FromError(getVideo);
                }

                Result<Stream> getStream = await _youTubeService.GetStreamAsync(getVideo.Entity, CancellationToken).ConfigureAwait(false);
                if (!getStream.IsSuccess)
                {
                    if (getStream.Error is YouTubeUserError yue)
                        return new GenericCommandError(yue.Message);

                    return Result.FromError(getStream);
                }

                // TODO: This will need to rely on the audio worker's cancellation token
                // For stop commands
                Stream pcmAudioStream = _musicService.ConvertToPcmInBackground(getStream.Entity, CancellationToken);

                // Give ffmpeg some time to start processing
                await Task.Delay(200, CancellationToken).ConfigureAwait(false);

                Result playResult = await _musicService.PlayAsync
                (
                    _context.GuildID.Value,
                    userVoiceState.Value.ChannelID.Value,
                    pcmAudioStream,
                    CancellationToken
                ).ConfigureAwait(false);

                var sendResult = await _feedbackService.SendContextualSuccessAsync("Finished playing!", ct: CancellationToken).ConfigureAwait(false);

                return !sendResult.IsSuccess
                    ? Result.FromError(sendResult)
                    : Result.FromSuccess();
            }
            catch (Exception ex)
            {
                // TODO: Proper logging and error message
                await _feedbackService.SendContextualErrorAsync(ex.ToString(), ct: CancellationToken).ConfigureAwait(false);
                return Result.FromError(new ExceptionError(ex));
            }
        }
    }
}
