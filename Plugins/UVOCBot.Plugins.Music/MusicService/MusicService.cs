using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Core;
using Remora.Discord.Voice;
using Remora.Discord.Voice.Abstractions.Services;
using Remora.Discord.Voice.Util;
using Remora.Results;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.Music.Abstractions.Services;

namespace UVOCBot.Plugins.Music.MusicService
{
    /// <inheritdoc cref="IMusicService"/>
    public class MusicService : IMusicService
    {
        private readonly DiscordVoiceClientFactory _voiceClientFactory;
        private readonly IServiceProvider _services;

        public MusicService
        (
            DiscordVoiceClientFactory voiceClientFactory,
            IServiceProvider services
        )
        {
            _voiceClientFactory = voiceClientFactory;
            _services = services;
        }

        /// <inheritdoc />
        public Stream ConvertToPcmInBackground(Stream input, CancellationToken ct = default)
        {
            Pipe p = new();

            FFMpegArguments.FromPipeInput(new StreamPipeSource(input))
                .OutputToPipe(new StreamPipeSink(p.Writer.AsStream()), options =>
                {
                    options.ForceFormat("s16le");
                    options.WithAudioSamplingRate(VoiceConstants.DiscordSampleRate);
                })
                .CancellableThrough(ct)
                .ProcessAsynchronously()
                .ContinueWith
                (
                    _ =>
                    {
                        p.Writer.Complete();
                        p.Reader.Complete();
                    },
                    ct
                );

            return p.Reader.AsStream();
        }

        /// <inheritdoc />
        public async Task<Result> PlayAsync(Snowflake guildID, Snowflake channelID, Stream pcmAudio, CancellationToken ct = default)
        {
            try
            {
                DiscordVoiceClient client = _voiceClientFactory.Get(guildID);

                Result runResult = await client.RunAsync
                (
                    guildID,
                    channelID,
                    false,
                    false,
                    ct
                ).ConfigureAwait(false);

                if (!runResult.IsSuccess)
                    return runResult;

                IAudioTranscoderService transcoder = _services.GetRequiredService<IAudioTranscoderService>();
                Result initialiseTranscoder = transcoder.Initialize();

                if (!initialiseTranscoder.IsSuccess)
                    return initialiseTranscoder;

                Result transmitResult = await client.TransmitAudioAsync
                (
                    pcmAudio,
                    transcoder,
                    ct
                ).ConfigureAwait(false);

                if (!transmitResult.IsSuccess)
                    return transmitResult;

                return Result.FromSuccess();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
