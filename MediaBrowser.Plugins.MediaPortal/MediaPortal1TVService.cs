using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Plugins.MediaPortal
{
    public class MediaPortal1TvService : BaseTunerHost, ITunerHost
    {
        public static MediaPortal1TvService Instance { get; private set; }

        public MediaPortal1TvService(IServerApplicationHost applicationHost) : base(applicationHost)
        {
            Instance = this;
        }

        public override string Name => Plugin.StaticName;

        public override string Type => "mediaportal";

        public override string SetupUrl => Plugin.GetPluginPageUrl(Type);

        public override bool SupportsGuideData(TunerHostInfo tuner)
        {
            return true;
        }

        public override TunerHostInfo GetDefaultConfiguration()
        {
            var tuner = base.GetDefaultConfiguration();

            tuner.Url = "http://localhost:4322";

            SetCustomOptions(tuner, new MediaPortalOptions());

            return tuner;
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<MediaPortalOptions>(tuner);
            var baseUrl = tuner.Url;

            var channels = await Plugin.TVService.GetChannels(baseUrl, config, cancellationToken).ConfigureAwait(false);

            foreach (var channel in channels)
            {
                channel.TunerHostId = tuner.Id;
                channel.Id = CreateEmbyChannelId(tuner, channel.Id);
            }

            return channels;
        }

        protected override async Task<List<ProgramInfo>> GetProgramsInternal(TunerHostInfo tuner, string tunerChannelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<MediaPortalOptions>(tuner);
            var baseUrl = tuner.Url;

            var programs = await Plugin.TVService.GetPrograms(baseUrl, config, tunerChannelId, startDateUtc, endDateUtc, cancellationToken).ConfigureAwait(false);

            foreach (var program in programs)
            {
                program.ChannelId = tunerChannelId;
                program.Id = GetProgramEntryId(program.ShowId, program.StartDate, program.ChannelId);
            }

            return programs;
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, BaseItem dbChannnel, ChannelInfo tunerChannel, CancellationToken cancellationToken)
        {
            var mediaPortalChannelId = GetTunerChannelIdFromEmbyChannelId(tuner, tunerChannel.Id);

            var mediaSourceInfo = new MediaSourceInfo
            {
                Id = "mediaportal_" + mediaPortalChannelId,
                Container = "ts",
                IsInfiniteStream = true,
                RequiresOpening = true,
                RequiresClosing = true,
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true
            };

            mediaSourceInfo.InferTotalBitrate();

            return Task.FromResult(new List<MediaSourceInfo> { mediaSourceInfo });
        }

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo tuner, BaseItem dbChannnel, ChannelInfo tunerChannel, string mediaSourceId, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<MediaPortalOptions>(tuner);

            var mediaPortalChannelId = GetTunerChannelIdFromEmbyChannelId(tuner, tunerChannel.Id);

            var mediaSource = await Plugin.StreamingService.GetLiveTvStream(tuner.Url, config, cancellationToken, tunerChannel.ChannelType, mediaPortalChannelId, mediaSourceId).ConfigureAwait(false);

            return LiveTvManager.CreateLiveStream(new LiveStreamOptions
            {
                MediaSource = mediaSource,
                TunerHost = tuner,
                OnClose = CloseLiveStream
            });
        }

        public Task CloseLiveStream(ILiveStream liveStream)
        {
            var tuner = LiveTvManager.GetTunerHostInfo(liveStream.TunerHostId);
            var config = GetProviderOptions<MediaPortalOptions>(tuner);
            var identifier = liveStream.MediaSource.Id;

            return Plugin.StreamingService.CancelStream(tuner.Url, config, CancellationToken.None, identifier);
        }

        public MediaPortalOptions GetConfiguration(TunerHostInfo tuner)
        {
            return GetProviderOptions<MediaPortalOptions>(tuner);
        }
    }
}