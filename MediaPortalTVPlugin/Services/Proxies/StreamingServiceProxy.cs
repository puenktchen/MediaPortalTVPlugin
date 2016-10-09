using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.MediaPortal.Helpers;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services.Proxies
{
    /// <summary>
    /// Provides access to the MP streaming functionality
    /// </summary>
    public class StreamingServiceProxy : ProxyBase
    {
        private readonly INetworkManager _networkManager;

        private String _streamingEndpoint = "StreamingService/stream";

        private const int STREAM_TIMEOUT_DIRECT = 30;
        private const int STREAM_TV_RECORDING_PROVIDER = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingServiceProxy"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="serialiser">The serialiser.</param>
        /// <param name="networkManager">The network manager.</param>
        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer serialiser, INetworkManager networkManager)
            : base(httpClient, serialiser)
        {
            _networkManager = networkManager;
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
        /// Gets the status information for the Streaming service
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public ServiceDescription GetStatusInfo(CancellationToken cancellationToken)
        {
            return GetFromService<ServiceDescription>(cancellationToken, "GetServiceDescription");
        }

        /// <summary>
        /// Gets the transcoder profiles supported
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public List<TranscoderProfile> GetTranscoderProfiles(CancellationToken cancellationToken)
        {
            return GetFromService<List<TranscoderProfile>>(cancellationToken, "GetTranscoderProfiles");
        }

        /// <summary>
        /// Gets a single transcoder profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public TranscoderProfile GetTranscoderProfile(CancellationToken cancellationToken, String name)
        {
            return GetFromService<TranscoderProfile>(cancellationToken, "GetTranscoderProfileByName?name={0}", name);
        }

        /// <summary>
        /// Gets the video stream for an existing recording
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recordingId">The recording id.</param>
        /// <param name="startPosition">The start position.</param>
        /// <returns></returns>
        public StreamingDetails GetRecordingStream(CancellationToken cancellationToken, String recordingId, TimeSpan startPosition)
        {
            return GetStream(cancellationToken, WebMediaType.Recording, recordingId, startPosition);
        }

        /// <summary>
        /// Gets a live tv stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="channelId">The channel to stream.</param>
        /// <returns></returns>
        public StreamingDetails GetLiveTvStream(CancellationToken cancellationToken, String channelId)
        {
            return GetStream(cancellationToken, WebMediaType.TV, channelId, TimeSpan.Zero);
        }
        
        /// <summary>
        /// Cancels an executing stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="streamIdentifier">The stream identifier.</param>
        /// <returns></returns>
        public bool CancelStream(CancellationToken cancellationToken, string streamIdentifier)
        {
            var result = GetFromService<WebBoolResult>(cancellationToken, "FinishStream?identifier={0}", streamIdentifier).Result;
            if (streamIdentifier.Contains("TV"))
            {
                result = Plugin.TvProxy.CancelCurrentTimeshifting(cancellationToken, streamIdentifier);
            }
            return result;
        }

        private StreamingDetails GetStream(CancellationToken cancellationToken, WebMediaType webMediaType, string itemId, TimeSpan startPosition)
        {
            Plugin.Logger.Info("Streaming setting RequiresAuthentication: {0}", Configuration.RequiresAuthentication);
            Plugin.Logger.Info("Streaming setting StreamingProfileName: {0}", Configuration.StreamingProfileName);
            Plugin.Logger.Info("Streaming setting StreamDelay: {0}", Configuration.StreamDelay);
            
            int mpextendedApiVersion = GetStatusInfo(cancellationToken).ApiVersion;

            var configuration = Plugin.Instance.Configuration;
            var profile = GetTranscoderProfile(cancellationToken, Configuration.StreamingProfileName);
            var identifier = HttpUtility.UrlEncode(String.Format("{0}-{1}-{2:yyyyMMddHHmmss}", webMediaType, itemId, DateTime.UtcNow));
            var url = "streamingURL";

            var isStreamInitialised = GetFromService<WebBoolResult>(cancellationToken,
                        "InitStream?type={0}&provider={1}&itemId={2}&identifier={3}&idleTimeout={4}&clientDescription={5}",
                        webMediaType,
                        STREAM_TV_RECORDING_PROVIDER,
                        itemId,
                        identifier,
                        STREAM_TIMEOUT_DIRECT,
                        identifier).Result;

            if (!isStreamInitialised)
            {
                throw new Exception(String.Format("Could not initialise the stream. Identifier={0}", identifier));
            }

            if (mpextendedApiVersion < 6 || configuration.RequiresAuthentication || !String.Equals(profile.Name, "Direct", StringComparison.OrdinalIgnoreCase))
            {
                url = GetFromService<WebStringResult>(cancellationToken, "StartStream?identifier={0}&profileName={1}&startPosition={2}",
                    identifier,
                    profile.Name,
                    (Int32)startPosition.TotalSeconds).Result; 
            }
            else
            {
                url = GetFromService<WebStringResult>(cancellationToken, "StartStream?identifier={0}&profileName={1}&startPosition={2}",
                    identifier,
                    profile.Name,
                    (Int32)startPosition.TotalSeconds).Result;

                url = GetUrl(_streamingEndpoint, "DoStream?type={0}&provider={1}&itemId={2}&clientDescription={3}&profileName={4}&startPosition={5}&idleTimeout={6}&identifier={7}",
                    webMediaType,
                    STREAM_TV_RECORDING_PROVIDER,
                    itemId,
                    identifier,
                    profile.Name,
                    (Int32)startPosition.TotalSeconds,
                    STREAM_TIMEOUT_DIRECT,
                    identifier);
            }

            var streamingDetails = new StreamingDetails()
            {
                StreamIdentifier = identifier,
                SourceInfo = new MediaSourceInfo()
                {
                    Path = url,
                    Protocol = MediaProtocol.Http,
                    Id = identifier, //itemId,
                }
            };

            System.Threading.Thread.Sleep(Plugin.Instance.Configuration.StreamDelay.Value);
            
            return streamingDetails;
        }

        /// <summary>
        /// Gets the recording image URL.
        /// </summary>
        /// <param name="recordingId">The recording id.</param>
        /// <returns></returns>
        public String GetRecordingImageUrl(String recordingId)
        {
            return GetUrl(_streamingEndpoint, "ExtractImage?type={0}&provider={1}&position={2}&itemId={3}",
                WebMediaType.Recording,
                STREAM_TV_RECORDING_PROVIDER,
                Configuration.PreviewThumbnailOffsetMinutes * 60,
                recordingId);
        }

        /// <summary>
        /// Gets the channel logo URL.
        /// </summary>
        /// <param name="channelId">The channel id.</param>
        /// <returns></returns>
        public String GetChannelLogoUrl(int channelId)
        {
            return GetUrl(_streamingEndpoint, "GetArtworkResized?id={0}&artworktype={1}&offset=0&mediatype={2}&maxWidth=160&maxHeight=160",
                    channelId, (Int32)WebFileType.Logo, (Int32)WebMediaType.TV);
        }
    }
}