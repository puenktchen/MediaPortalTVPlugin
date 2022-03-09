using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Plugins.MediaPortal
{
    /// <summary>
    /// Provides MP (v1) integration for Emby
    /// </summary>
    public class MediaPortal1TvService : BaseTunerHost
    {
        public MediaPortal1TvService(IServerApplicationHost applicationHost)
            : base(applicationHost)
        {
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

            var channels = await Plugin.TvProxy.GetChannels(baseUrl, config, cancellationToken).ConfigureAwait(false);

            foreach (var channel in channels)
            {
                channel.TunerHostId = tuner.Id;
                channel.Id = CreateEmbyChannelId(tuner, channel.Id);
            }

            return channels.ToList();
        }

        protected override async Task<List<ProgramInfo>> GetProgramsInternal(TunerHostInfo tuner, string tunerChannelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<MediaPortalOptions>(tuner);
            var baseUrl = tuner.Url;

            var list = await Plugin.TvProxy.GetPrograms(baseUrl, config, tunerChannelId, startDateUtc, endDateUtc, cancellationToken).ConfigureAwait(false);

            foreach (var item in list)
            {
                item.ChannelId = tunerChannelId;
                item.Id = GetProgramEntryId(item.ShowId, item.StartDate, item.ChannelId);
            }

            return list;
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, BaseItem dbChannnel, ChannelInfo tunerChannel, CancellationToken cancellationToken)
        {
            var mediaSource = new MediaSourceInfo
            {
                // this is dummy info and will be opened in GetChannelStream, based on our setting of RequiresOpening=true
                // just make sure that it is predictable and returns the same result each time
                Path = "http://mp-direct/" + tunerChannel.Id.GetMD5().ToString("N"),
                Protocol = MediaProtocol.Http,

                RequiresOpening = true,
                RequiresClosing = true,

                Container = "ts",
                Id = "direct",

                // this needs review but I'm not sure these values matter at this earlier stage
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true,

                IsInfiniteStream = true
            };

            mediaSource.InferTotalBitrate();

            return Task.FromResult(new List<MediaSourceInfo> { mediaSource });
        }

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo tuner, BaseItem dbChannnel, ChannelInfo tunerChannel, string mediaSourceId, CancellationToken cancellationToken)
        {
            var config = GetProviderOptions<MediaPortalOptions>(tuner);
            var baseUrl = tuner.Url;

            var mediaPortalChannelId = GetTunerChannelIdFromEmbyChannelId(tuner, tunerChannel.Id);

            var mediaSource = await Plugin.StreamingProxy.GetLiveTvStream(baseUrl, config, cancellationToken, mediaPortalChannelId, mediaSourceId).ConfigureAwait(false);

            return LiveTvManager.CreateLiveStream(new LiveStreamOptions
            {
                MediaSource = mediaSource,
                TunerHost = tuner,
                OnClose = CloseLiveStream
            });
        }

        public Task CloseLiveStream(ILiveStream liveStream)
        {
            // TODO: Need a way to get the TunerHostInfo object

            return Task.CompletedTask;
            //var tuner = liveStream.TunerHostId;
            //var config = GetProviderOptions<MediaPortalOptions>(tuner);
            //var liveStreamId = liveStream.OriginalStreamId;

            //var baseUrl = tuner.Url;

            //return Plugin.StreamingProxy.CancelStream(baseUrl, config, CancellationToken.None, liveStreamId);
        }
    }
}