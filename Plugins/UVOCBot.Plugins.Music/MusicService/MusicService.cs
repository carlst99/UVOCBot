using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Voice;
using Remora.Discord.Voice.Util;
using Remora.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Discord.Core;
using UVOCBot.Discord.Core.Errors;
using UVOCBot.Discord.Core.Services.Abstractions;
using UVOCBot.Plugins.Music.Abstractions.Services;
using UVOCBot.Plugins.Music.Objects;

namespace UVOCBot.Plugins.Music.MusicService
{
    /// <inheritdoc cref="IMusicService"/>
    public class MusicService : IMusicService
    {
        protected class PlaybackState : IDisposable
        {
            public MusicRequest Request { get; }
            public Task<Result> TransmissionTask { get; }
            public Task ConversionTask { get; }
            public CancellationTokenSource CancellationToken { get; }

            public bool IsCompleted => TransmissionTask.IsCompleted && ConversionTask.IsCompleted;

            public PlaybackState
            (
                MusicRequest request,
                Task<Result> transmissionTask,
                Task conversionTask,
                CancellationTokenSource cancellationToken
            )
            {
                Request = request;
                TransmissionTask = transmissionTask;
                ConversionTask = conversionTask;
                CancellationToken = cancellationToken;
            }

            public void Dispose()
            {
                CancellationToken.Dispose();
            }
        }

        private readonly ILogger<MusicService> _logger;
        private readonly ConcurrentDictionary<Snowflake, ConcurrentQueue<MusicRequest>> _playQueues;
        private readonly ConcurrentDictionary<Snowflake, PlaybackState> _currentlyPlaying;
        private readonly ConcurrentDictionary<Snowflake, DateTimeOffset> _activeClients;

        protected readonly DiscordVoiceClientFactory _voiceClientFactory;
        protected readonly IPermissionChecksService _permissionChecksService;
        protected readonly IYouTubeService _youTubeService;
        protected readonly IDiscordRestChannelAPI _channelApi;

        public MusicService
        (
            ILogger<MusicService> logger,
            DiscordVoiceClientFactory voiceClientFactory,
            IPermissionChecksService permissionChecksService,
            IYouTubeService youTubeService,
            IDiscordRestChannelAPI channelApi
        )
        {
            _logger = logger;
            _voiceClientFactory = voiceClientFactory;
            _permissionChecksService = permissionChecksService;
            _youTubeService = youTubeService;
            _channelApi = channelApi;

            _playQueues = new ConcurrentDictionary<Snowflake, ConcurrentQueue<MusicRequest>>();
            _currentlyPlaying = new ConcurrentDictionary<Snowflake, PlaybackState>();
            _activeClients = new ConcurrentDictionary<Snowflake, DateTimeOffset>();
        }

        /// <inheritdoc />
        public async Task<Result> RunAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CancelCurrentlyPlayingStates(true).ConfigureAwait(false);
                    await StopActiveClients(true).ConfigureAwait(false);

                    foreach (Snowflake guildID in _playQueues.Keys)
                    {
                        if (_currentlyPlaying.ContainsKey(guildID))
                            continue;

                        if (!_playQueues.TryGetValue(guildID, out ConcurrentQueue<MusicRequest>? queue))
                            continue;

                        if (!queue.TryDequeue(out MusicRequest? request))
                            continue;

                        Result playResult = await PlayAsync(request, ct).ConfigureAwait(false);
                        if (!playResult.IsSuccess)
                        {
                            Embed errorEmbed = new
                            (
                                Description: "Something went wrong when playing " + Formatter.Bold(request.Video.Title),
                                Colour: Color.Red
                            );

                            await _channelApi.CreateMessageAsync
                            (
                                request.ContextChannelID,
                                embeds: new[] { errorEmbed },
                                ct: ct
                            ).ConfigureAwait(false);

                            _logger.LogError("The music service failed to play a song: {error}", playResult.Error);
                            continue;
                        }

                        Embed responseEmbed = new
                        (
                            $"Playing {Formatter.Bold(request.Video.Title)} ({request.Video.Duration:mm\\:ss})",
                            Description: request.Video.Author.Title + "\n" + request.Video.Url,
                            Thumbnail: new EmbedThumbnail(request.Video.Thumbnails[0].Url),
                            Colour: DiscordConstants.DEFAULT_EMBED_COLOUR
                        );

                        await _channelApi.CreateMessageAsync
                        (
                            request.ContextChannelID,
                            embeds: new[] { responseEmbed },
                            ct: ct
                        ).ConfigureAwait(false);
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    _logger.LogWarning(ex, "The music service encountered an error.");
                }
            }

            await CancelCurrentlyPlayingStates(false).ConfigureAwait(false);
            await StopActiveClients(false).ConfigureAwait(false);

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> HasMusicPermissionsAsync(Snowflake channelID, CancellationToken ct = default)
        {
            Result<IDiscordPermissionSet> channelPermissions = await _permissionChecksService.GetPermissionsInChannel
                (
                    channelID,
                    DiscordConstants.UserId,
                    ct
                ).ConfigureAwait(false);

            if (!channelPermissions.IsDefined())
                return Result.FromError(channelPermissions);

            if (!channelPermissions.Entity.HasAdminOrPermission(DiscordPermission.Connect))
                return new PermissionError(DiscordPermission.Connect, DiscordConstants.UserId, channelID);

            if (!channelPermissions.Entity.HasAdminOrPermission(DiscordPermission.Speak))
                return new PermissionError(DiscordPermission.Speak, DiscordConstants.UserId, channelID);

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public void Enqueue(MusicRequest request)
        {
            if (!_playQueues.ContainsKey(request.GuildID))
                _playQueues.TryAdd(request.GuildID, new ConcurrentQueue<MusicRequest>());

            _playQueues[request.GuildID].Enqueue(request);
        }

        /// <inheritdoc />
        public async Task SkipAsync(Snowflake guildID, int amount = 1, CancellationToken ct = default)
        {
            if (!_playQueues.TryGetValue(guildID, out ConcurrentQueue<MusicRequest>? queue))
                return;

            int startIndex = 0;

            if (_currentlyPlaying.TryRemove(guildID, out PlaybackState? state))
            {
                startIndex = 1;
                await CancelStateAsync(state).ConfigureAwait(false);
            }

            for (int i = startIndex; i < amount && i < queue.Count; i++)
                queue.TryDequeue(out _);
        }

        /// <inheritdoc />
        public void ClearQueue(Snowflake guildID)
        {
            _playQueues.TryRemove(guildID, out _);
        }

        /// <inheritdoc />
        public IReadOnlyList<MusicRequest> GetQueue(Snowflake guildID)
        {
            return _playQueues[guildID].ToArray();
        }

        /// <inheritdoc />
        public MusicRequest? GetCurrentlyPlaying(Snowflake guildID)
        {
            if (_currentlyPlaying.TryGetValue(guildID, out PlaybackState? state))
                return state.Request;

            return null;
        }

        /// <inheritdoc />
        public async Task<Result> ForceDisconnectAsync(Snowflake guildID, CancellationToken ct = default)
        {
            try
            {
                if (!_currentlyPlaying.ContainsKey(guildID))
                    return Result.FromSuccess();

                if (_currentlyPlaying.TryRemove(guildID, out PlaybackState? state))
                    await CancelStateAsync(state).ConfigureAwait(false);

                DiscordVoiceClient client = _voiceClientFactory.Get(guildID);

                if (client.IsRunning)
                    return await client.StopAsync().ConfigureAwait(false);

                return Result.FromSuccess();
            }
            finally
            {
                _currentlyPlaying.TryRemove(guildID, out _);
            }
        }

        /// <summary>
        /// Cancels and disposes of a playback state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task CancelStateAsync(PlaybackState state, [CallerMemberName] string? caller = null)
        {
            if (!state.CancellationToken.IsCancellationRequested)
                state.CancellationToken.Cancel();

            Console.WriteLine("===== Cancelling: " + caller); // TODO: Remove caller

            Result transmitResult = await state.TransmissionTask.ConfigureAwait(false);
            if (!transmitResult.IsSuccess)
                _logger.LogWarning("A transmission task failed: {error}", transmitResult.Error);

            state.Dispose();
        }

        /// <summary>
        /// Cancels playback states held in <see cref="_currentlyPlaying"/>.
        /// </summary>
        /// <param name="onlyFinished">A value indicating if the states should only be canceled if they are finished.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task CancelCurrentlyPlayingStates(bool onlyFinished)
        {
            foreach (Snowflake guildID in _currentlyPlaying.Keys)
            {
                if (!_currentlyPlaying.TryGetValue(guildID, out PlaybackState? state))
                    continue;

                if (!state.IsCompleted && onlyFinished)
                    continue;

                await CancelStateAsync(state).ConfigureAwait(false);
                _currentlyPlaying.TryRemove(guildID, out _);
                _activeClients[guildID] = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>
        /// Stops active voice clients.
        /// </summary>
        /// <param name="onlyOld">A value indicating whether to only stop clients that have not been used in the last 2m.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task StopActiveClients(bool onlyOld)
        {
            foreach (Snowflake guildID in _activeClients.Keys)
            {
                if (!_activeClients.TryGetValue(guildID, out DateTimeOffset lastAction))
                    continue;

                DiscordVoiceClient client = _voiceClientFactory.Get(guildID);

                if (lastAction.AddMinutes(2) > DateTimeOffset.UtcNow && onlyOld && client.IsTransmitting)
                    continue;

                Result stopResult = await client.StopAsync().ConfigureAwait(false);
                if (!stopResult.IsSuccess)
                    _logger.LogWarning("A voice client failed to stop: {error}", stopResult.Error);

                _activeClients.TryRemove(guildID, out _);
            }
        }

        /// <summary>
        /// Plays a video.
        /// </summary>
        /// <param name="request">The request to play.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
        /// <returns>A result representing the outcome of the operation.</returns>
        protected async Task<Result> PlayAsync(MusicRequest request, CancellationToken ct = default)
        {
            try
            {
                DiscordVoiceClient client = _voiceClientFactory.Get(request.GuildID);

                if (!client.IsRunning)
                {
                    Result runResult = await client.RunAsync
                    (
                        request.GuildID,
                        request.ChannelID,
                        false,
                        true,
                        ct
                    ).ConfigureAwait(false);

                    if (!runResult.IsSuccess)
                        return runResult;

                    _activeClients.TryAdd(request.GuildID, DateTimeOffset.UtcNow);
                }

                Result<Stream> ytStream = await _youTubeService.GetStreamAsync(request.Video, ct).ConfigureAwait(false);
                if (!ytStream.IsSuccess)
                    return Result.FromError(ytStream);

                CancellationTokenSource cts = new();
                (Stream Stream, Task Task) pcmStream = ConvertToPcmInBackground(ytStream.Entity, cts.Token);

                while (pcmStream.Task.Status != TaskStatus.Running)
                    await Task.Delay(10, ct).ConfigureAwait(false);

                // Give the converter some time to start pushing data
                await Task.Delay(200, ct).ConfigureAwait(false);
                Task<Result> transmitTask = client.TransmitAudioAsync(pcmStream.Stream, ct: cts.Token);

                PlaybackState state = new(request, transmitTask, pcmStream.Task, cts);
                _currentlyPlaying.AddOrUpdate
                (
                    request.GuildID,
                    state,
                    (_, _) => state
                );

                return Result.FromSuccess();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// Converts an audio stream to PCM-16, using ffmpeg.
        /// </summary>
        /// <param name="input">The audio stream to convert.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the underlying ffmpeg conversion.</param>
        /// <returns>The stream of audio. It will not be complete when this method returns.</returns>
        protected (Stream, Task) ConvertToPcmInBackground(Stream input, CancellationToken ct = default)
        {
            Pipe p = new();

            Task convertTask = new
            (
                () =>
                {
                    try
                    {
                        FFMpegArguments.FromPipeInput(new StreamPipeSource(input))
                            .OutputToPipe(new StreamPipeSink(p.Writer.AsStream()), options =>
                            {
                                options.ForceFormat("s16le");
                                options.WithAudioSamplingRate(VoiceConstants.DiscordSampleRate);
                            })
                            .CancellableThrough(ct)
                            .ProcessSynchronously();
                    }
                    catch (Exception ex) when (ex is not TaskCanceledException)
                    {
                        _logger.LogError(ex, "Failed to convert youtube stream.");
                    }
                    finally
                    {
                        p.Writer.Complete();
                    }
                },
                ct,
                TaskCreationOptions.LongRunning
            );

            convertTask.Start();

            return (p.Reader.AsStream(), convertTask);
        }
    }
}
