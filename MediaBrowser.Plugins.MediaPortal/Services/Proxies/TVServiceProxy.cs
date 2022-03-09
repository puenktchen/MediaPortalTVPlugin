using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.MediaPortal.Helpers;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services.Proxies
{
    /// <summary>
    /// Provides access to the MP tv service functionality
    /// </summary>
    public class TvServiceProxy : ProxyBase
    {
        private readonly StreamingServiceProxy _wssProxy;

        public TvServiceProxy(IHttpClient httpClient, IJsonSerializer serialiser, StreamingServiceProxy wssProxy)
            : base(httpClient, serialiser)
        {
            _wssProxy = wssProxy;
        }

        protected override string EndPointSuffix
        {
            get { return "TVAccessService/json"; }
        }

        #region Get Methods

        public async Task<List<ChannelInfo>> GetChannels(string url, MediaPortalOptions configuration, CancellationToken cancellationToken)
        {
            var tvChannels = await GetFromServiceAsync<List<Channel>>(url, configuration, cancellationToken, "GetChannelsDetailed").ConfigureAwait(false);
            var radioChannels = await GetFromServiceAsync<List<Channel>>(url, configuration, cancellationToken, "GetRadioChannelsDetailed").ConfigureAwait(false);

            var channels = tvChannels.Concat(radioChannels).Where(c => c.VisibleInGuide).ToList();

            Plugin.Logger.Info("Found overall channels: {0}", channels.Count);
            return channels.Select((c, index) =>
            {
                var channel = new ChannelInfo()
                {
                    Id = c.Id,
                    Name = c.Title,
                    ChannelType = ChannelType.TV,
                    ImageUrl = _wssProxy.GetChannelLogo(url, configuration, c),
                };

                return channel;

            }).ToList();
        }

        public async Task<List<ProgramInfo>> GetPrograms(string url, MediaPortalOptions configuration, string channelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var response = await GetFromServiceAsync<List<Program>>(url, configuration,
                cancellationToken,
                "GetProgramsDetailedForChannel?channelId={0}&startTime={1}&endTime={2}",
                channelId,
                startDateUtc.ToLocalTime().ToUrlDate(),
                endDateUtc.ToLocalTime().ToUrlDate()).ConfigureAwait(false);

            Plugin.Logger.Info("Found programs: {0}  for channel id: {1}", response.Count(), channelId);
            return response.Select(p =>
            {
                var program = new ProgramInfo()
                {
                    Name = p.Title,
                    EpisodeNumber = p.EpisodeNumber,
                    SeasonNumber = p.SeasonNumber,
                    ProductionYear = p.ProductionYear,
                    Id = p.Id.ToString(CultureInfo.InvariantCulture),
                    SeriesId = p.Title,
                    ChannelId = channelId,
                    StartDate = p.StartTime,
                    EndDate = p.EndTime,
                    Overview = p.Description,
                    Genres = new List<String>(),
                };

                program.IsSeries = true;
                if (!String.IsNullOrEmpty(p.Genre))
                {
                    program.Genres.Add(p.Genre);
                }

                if (program.IsSeries && p.Title != p.EpisodeName)
                {
                    program.EpisodeTitle = p.EpisodeName;
                }

                return program;

            }).ToList();
        }

        #endregion
    }
}