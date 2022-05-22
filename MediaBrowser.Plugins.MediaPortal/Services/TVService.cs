using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;

using MediaBrowser.Plugins.MediaPortal.Helpers;
using MediaBrowser.Plugins.MediaPortal.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services
{
    public class TVService : ProxyService
    {
        public TVService(IHttpClient httpClient, IJsonSerializer serialiser) : base(httpClient, serialiser)
        {
        }

        protected override string EndPointSuffix
        {
            get { return "TVAccessService/json"; }
        }

        public List<ChannelGroup> GetTvChannelGroups(string url, MediaPortalOptions configuration, CancellationToken cancellationToken)
        {
            return GetFromServiceAsync<List<ChannelGroup>>(url, configuration, cancellationToken, "GetGroups").Result.OrderBy(g => g.SortOrder).ToList();
        }

        public List<ChannelGroup> GetRadioChannelGroups(string url, MediaPortalOptions configuration, CancellationToken cancellationToken)
        {
            return GetFromServiceAsync<List<ChannelGroup>>(url, configuration, cancellationToken, "GetRadioGroups").Result.OrderBy(g => g.SortOrder).ToList();
        }

        public async Task<List<ChannelInfo>> GetChannels(string url, MediaPortalOptions configuration, CancellationToken cancellationToken)
        {
            var tvChannels = new List<Channel>();
            var radioChannels = new List<Channel>();

            if (!string.IsNullOrWhiteSpace(configuration.TvChannelGroup))
            {
                tvChannels = await GetFromServiceAsync<List<Channel>>(url, configuration, cancellationToken, "GetChannelsBasic?GroupId={0}", configuration.TvChannelGroup).ConfigureAwait(false);
            }
            
            if (!string.IsNullOrWhiteSpace(configuration.RadioChannelGroup))
            {
                if (configuration.ImportRadioChannels)
                {
                    radioChannels = await GetFromServiceAsync<List<Channel>>(url, configuration, cancellationToken, "GetRadioChannelsBasic?GroupId={0}", configuration.RadioChannelGroup).ConfigureAwait(false);
                }
            }

            var channels = tvChannels.Concat(radioChannels).ToList();

            return channels.Select((c, index) =>
            {
                var channel = new ChannelInfo()
                {
                    Id = c.Id,
                    Name = c.Title,
                    Number = (index + 1).ToString(),
                    ChannelType = c.IsTv ? ChannelType.TV : ChannelType.Radio,
                    ImageUrl = Plugin.StreamingService.GetChannelLogo(url, configuration, c.IsTv,c.Id),
                };

                return channel;

            }).ToList();
        }

        public async Task<List<ProgramInfo>> GetPrograms(string url, MediaPortalOptions configuration, string channelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var genreMapper = new GenreMapper(configuration);

            var response = await GetFromServiceAsync<List<Program>>(url, configuration, cancellationToken, "GetProgramsDetailedForChannel?channelId={0}&startTime={1}&endTime={2}",
                channelId,
                startDateUtc.ToLocalTime().ToString("s"),
                endDateUtc.ToLocalTime().ToString("s")).ConfigureAwait(false);

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
                    Genres = new List<string>(),
                    IsSeries = true
                };

                if (!string.IsNullOrEmpty(p.Genre))
                {
                    program.Genres.Add(p.Genre);
                    genreMapper.SetProgramCategories(program);
                }

                if (program.IsSeries && p.Title != p.EpisodeName)
                {
                    program.EpisodeTitle = p.EpisodeName;
                }

                var cachePath = Path.Combine(Plugin.ConfigurationManager.CommonApplicationPaths.CachePath, "mediaportal");
                var logoImage = Path.Combine(cachePath, channelId + "-logo.png");
                var landscapeImage = Path.Combine(cachePath, channelId + "-landscape.png");
                var posterImage = Path.Combine(cachePath, channelId + "-poster.png");

                if (File.Exists(logoImage))
                    program.LogoImageUrl = logoImage;
                if (File.Exists(landscapeImage))
                    program.ThumbImageUrl = landscapeImage;
                if (File.Exists(posterImage))
                    program.ImageUrl = posterImage;

                return program;

            }).ToList();
        }
    }
}