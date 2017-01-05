using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;

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
            Plugin.Logger.Info("Streaming Media Type: {0}; Streaming item ID: {1}", webMediaType, itemId);

            var configuration = Plugin.Instance.Configuration;
            var profile = GetTranscoderProfile(cancellationToken, Configuration.StreamingProfileName);
            var identifier = HttpUtility.UrlEncode(String.Format("{0}-{1}-{2:yyyyMMddHHmmss}", webMediaType, itemId, DateTime.UtcNow));
            var url = "Streaming URL or Recording Path";

            var streamingDetails = new StreamingDetails()
            {
                StreamIdentifier = identifier,
                SourceInfo = new MediaSourceInfo()
                {
                    Id = identifier, //itemId,
                    ReadAtNativeFramerate = true,
                    IsInfiniteStream = true,
                }
            };

            if (webMediaType == WebMediaType.Recording && configuration.EnableDirectAccess)
            {
                url = Plugin.TvProxy.GetRecording(cancellationToken, itemId).FileName;

                streamingDetails.SourceInfo.Path = url;
                streamingDetails.SourceInfo.Protocol = MediaProtocol.File;

                if (configuration.RequiresPathSubstitution)
                {
                    url = url.Replace(configuration.LocalFilePath, configuration.RemoteFilePath);
                }
            }
            else
            {
                url = GetUrl(_streamingEndpoint, "DoStream?type={0}&provider={1}&itemId={2}&clientDescription={3}&profileName={4}&startPosition={5}&idleTimeout={6}&identifier={7}",
                        webMediaType,
                        STREAM_TV_RECORDING_PROVIDER,
                        itemId,
                        identifier,
                        profile.Name,
                        (Int32)startPosition.TotalSeconds,
                        STREAM_TIMEOUT_DIRECT,
                        identifier);

                streamingDetails.SourceInfo.Path = url;
                streamingDetails.SourceInfo.Protocol = MediaProtocol.Http;

                if (configuration.RequiresAuthentication)
                {
                    string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password);
                    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

                    streamingDetails.SourceInfo.SupportsDirectPlay = false;
                    streamingDetails.SourceInfo.RequiredHttpHeaders = new Dictionary<string, string> { { "Authentication", "Basic " + authInfo } };
                }

                System.Threading.Thread.Sleep(Plugin.Instance.Configuration.StreamDelay.Value);
            }

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
            return GetUrl(_streamingEndpoint, "GetArtwork?id={0}&artworktype={1}&offset=0&mediatype={2}",
                    channelId, (Int32)WebFileType.Logo, (Int32)WebMediaType.TV);
        }
    }
}