using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services.Proxies
{
    /// <summary>
    /// Provides access to the MP streaming functionality
    /// </summary>
    public class StreamingServiceProxy : ProxyBase
    {
        private String _streamingEndpoint = "StreamingService/stream";

        private const int STREAM_TIMEOUT_DIRECT = 30;
        private const int STREAM_TV_RECORDING_PROVIDER = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingServiceProxy"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="serialiser">The serialiser.</param>
        /// <param name="networkManager">The network manager.</param>
        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer serialiser)
            : base(httpClient, serialiser)
        {
        }

        /// <summary>
        /// Gets the end point suffix.
        /// </summary>
        /// <value>
        /// The end point suffix.
        /// </value>
        /// <remarks>
        /// The value appended after "MPExtended" on the service url
        /// </remarks>
        protected override string EndPointSuffix
        {
            get { return "StreamingService/json"; }
        }

        /// <summary>
        /// Gets a single transcoder profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Task<TranscoderProfile> GetTranscoderProfile(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, String name)
        {
            return GetFromServiceAsync<TranscoderProfile>(url, configuration, cancellationToken, "GetTranscoderProfileByName?name={0}", name);
        }

        /// <summary>
        /// Gets a live tv stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="channelId">The channel to stream.</param>
        /// <returns></returns>
        public async Task<MediaSourceInfo> GetLiveTvStream(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, String channelId, string mediaSourceId)
        {
            Plugin.Logger.Info("Streaming Media Type: {0}; Streaming item ID: {1}", WebMediaType.TV, channelId);

            var identifier = WebUtility.UrlEncode(String.Format("{0}-{1}-{2:yyyyMMddHHmmss}", WebMediaType.TV, channelId, DateTimeOffset.UtcNow));

            var transcodingProfile = await GetTranscoderProfile(url, configuration, cancellationToken, mediaSourceId).ConfigureAwait(false);

            var streamingDetails = new MediaSourceInfo()
            {
                RequiresClosing = true
            };

            streamingDetails.Id = identifier;

            var authorized = await GetFromServiceAsync<WebBoolResult>(url, configuration, cancellationToken, "AuthorizeStreaming").ConfigureAwait(false);

            streamingDetails.Protocol = MediaProtocol.Http;

            streamingDetails.IsInfiniteStream = true;
            streamingDetails.Path = GetUrl(url, configuration, _streamingEndpoint, "DoStream?type={0}&provider={1}&itemId={2}&clientDescription={3}&profileName={4}&startPosition=0&idleTimeout={5}&identifier={6}",
                    WebMediaType.TV,
                    STREAM_TV_RECORDING_PROVIDER,
                    channelId,
                    identifier,
                    WebUtility.UrlEncode(transcodingProfile.Name),
                    STREAM_TIMEOUT_DIRECT,
                    identifier);

            if (!string.IsNullOrEmpty(configuration.UserName))
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

                streamingDetails.SupportsDirectPlay = false;
                streamingDetails.RequiredHttpHeaders = new Dictionary<string, string> { { "Authentication", "Basic " + authInfo } };
            }

            Plugin.Logger.Info("Returning StreamingDetails for: {0}", streamingDetails.Path);
            return streamingDetails;
        }

        /// <summary>
        /// Cancels an executing stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="streamIdentifier">The stream identifier.</param>
        /// <returns></returns>
        public Task CancelStream(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, string streamIdentifier)
        {
            return GetFromServiceAsync<WebBoolResult>(url, configuration, cancellationToken, "FinishStream?identifier={0}", streamIdentifier);
        }

        /// <summary>
        /// Gets the channel logo.
        /// </summary>
        /// <param name="channelId">The channel id.</param>
        /// <returns></returns>
        public String GetChannelLogo(string url, MediaPortalOptions configuration, Channel channel)
        {
            return GetUrl(url, configuration, _streamingEndpoint, "GetArtwork?id={0}&artworktype={1}&offset=0&mediatype={2}", channel.Id, 5, (Int32)WebMediaType.TV);
        }
    }
}