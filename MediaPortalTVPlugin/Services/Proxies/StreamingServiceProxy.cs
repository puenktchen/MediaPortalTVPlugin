using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dto;
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
        /// <param name="recordingId">The recording id.</param>
        /// <returns></returns>
        public String GetRecordingStream(String recordingId)
        {
                Plugin.Logger.Info("Streaming setting RequiresAuthentication: {0}", Configuration.RequiresAuthentication);
                Plugin.Logger.Info("Streaming setting StreamingProfileName: {0}", Configuration.StreamingProfileName);
                Plugin.Logger.Info("Streaming Media Type: {0}; Streaming item ID: {1}", WebMediaType.Recording, recordingId);

                var identifier = WebUtility.UrlEncode(String.Format("{0}-{1}-{2:yyyyMMddHHmmss}", WebMediaType.Recording, recordingId, DateTime.UtcNow));

                return GetUrl(_streamingEndpoint, "DoStream?type={0}&itemId={1}&profileName={2}&startPosition=0&idleTimeout=5&identifier={3}",
                            WebMediaType.Recording,
                            recordingId,
                            Configuration.StreamingProfileName,
                            identifier);
        }

        /// <summary>
        /// Gets a live tv stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="channelId">The channel to stream.</param>
        /// <returns></returns>
        public StreamingDetails GetLiveTvStream(CancellationToken cancellationToken, String channelId)
        {
            Plugin.Logger.Info("Streaming setting RequiresAuthentication: {0}", Configuration.RequiresAuthentication);
            Plugin.Logger.Info("Streaming setting StreamingProfileName: {0}", Configuration.StreamingProfileName);
            Plugin.Logger.Info("Streaming setting StreamDelay: {0}", Configuration.StreamDelay);
            Plugin.Logger.Info("Streaming Media Type: {0}; Streaming item ID: {1}", WebMediaType.TV, channelId);

            var identifier = WebUtility.UrlEncode(String.Format("{0}-{1}-{2:yyyyMMddHHmmss}", WebMediaType.TV, channelId, DateTime.UtcNow));
            var profile = WebUtility.UrlEncode(GetTranscoderProfile(cancellationToken, Configuration.StreamingProfileName).Name);

            var streamingDetails = new StreamingDetails()
            {
                SourceInfo = new MediaSourceInfo()
            };

            streamingDetails.StreamIdentifier = identifier;
            streamingDetails.SourceInfo.Id = identifier;

            if (Configuration.RtspStreaming)
            {
                streamingDetails.SourceInfo.Protocol = MediaProtocol.Rtsp;
                streamingDetails.SourceInfo.ReadAtNativeFramerate = false;
                streamingDetails.SourceInfo.IsInfiniteStream = true;
                streamingDetails.SourceInfo.SupportsProbing = (Configuration.EnableProbing) ? true : false;
                streamingDetails.SourceInfo.Path = Plugin.TvProxy.GetLiveTvRtspUrl(cancellationToken, identifier, channelId);
            }
            else
            {
                var authorized = GetFromService<WebBoolResult>(cancellationToken, "AuthorizeStreaming");

                streamingDetails.SourceInfo.Protocol = MediaProtocol.Http;
                streamingDetails.SourceInfo.ReadAtNativeFramerate = true;
                streamingDetails.SourceInfo.IsInfiniteStream = true;
                streamingDetails.SourceInfo.SupportsProbing = (Configuration.EnableProbing) ? true : false;
                streamingDetails.SourceInfo.Path = GetUrl(_streamingEndpoint, "DoStream?type={0}&provider={1}&itemId={2}&clientDescription={3}&profileName={4}&startPosition=0&idleTimeout={5}&identifier={6}",
                        WebMediaType.TV,
                        STREAM_TV_RECORDING_PROVIDER,
                        channelId,
                        identifier,
                        profile,
                        STREAM_TIMEOUT_DIRECT,
                        identifier);

                if (Configuration.RequiresAuthentication)
                {
                    string authInfo = String.Format("{0}:{1}", Configuration.UserName, Configuration.Password);
                    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

                    streamingDetails.SourceInfo.SupportsDirectPlay = false;
                    streamingDetails.SourceInfo.RequiredHttpHeaders = new Dictionary<string, string> { { "Authentication", "Basic " + authInfo } };
                }
            }

            Thread.Sleep(Plugin.Instance.Configuration.StreamDelay.Value);

            Plugin.Logger.Info("Returning StreamingDetails for: {0}", streamingDetails.SourceInfo.Path);
            return streamingDetails;
        }
        
        /// <summary>
        /// Cancels an executing stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="streamIdentifier">The stream identifier.</param>
        /// <returns></returns>
        public bool CancelStream(CancellationToken cancellationToken, string streamIdentifier)
        {
            if (Configuration.RtspStreaming && streamIdentifier.StartsWith("TV"))
            {
                return Plugin.TvProxy.CancelCurrentTimeshifting(cancellationToken, streamIdentifier);
            }
            else
            {
                if (!String.Equals(Configuration.StreamingProfileName, "Direct", StringComparison.OrdinalIgnoreCase))
                {
                    var ffmpeg = Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains("ffmpeg"));
                    foreach (var process in ffmpeg.Where(p => p.MainModule.FileName.ToLower().Contains("mpextended")))
                    {
                        process.Kill();
                    }
                }
                return GetFromService<WebBoolResult>(cancellationToken, "FinishStream?identifier={0}", streamIdentifier).Result;
            }
        }

        /// <summary>
        /// Gets the recording image URL.
        /// </summary>
        /// <param name="recordingId">The recording id.</param>
        /// <returns></returns>
        public String GetRecordingImage(String recordingId)
        {
            return GetUrl(_streamingEndpoint, "ExtractImage?type={0}&position={1}&itemId={2}", WebMediaType.Recording, Configuration.PreviewThumbnailOffsetMinutes * 60, recordingId);
        }

        /// <summary>
        /// Gets the channel logo.
        /// </summary>
        /// <param name="channelId">The channel id.</param>
        /// <returns></returns>
        public String GetChannelLogo(Channel channel)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var remoteUrl = GetUrl(_streamingEndpoint, "GetArtwork?id={0}&artworktype={1}&offset=0&mediatype={2}", channel.Id, (Int32)WebFileType.Logo, (Int32)WebMediaType.TV);
            var localImagePath = Path.Combine(pluginPath, "channellogos", channel.Id + ".png");
            var localLandscapePath = Path.Combine(pluginPath, "channellogos", channel.Id + "-landscape.png");
            var localPosterPath = Path.Combine(pluginPath, "channellogos", channel.Id + "-poster.png");
            var localLogoPath = Path.Combine(pluginPath, "channellogos", channel.Id + "-logo.png");

            if (Configuration.ProgramImages || Configuration.RequiresAuthentication)
            {

                if (!Directory.Exists(Path.Combine(pluginPath, "channellogos")))
                {
                    Directory.CreateDirectory(Path.Combine(pluginPath, "channellogos"));
                }

                try
                {
                    using (WebClient client = new WebClient())
                    {
                        if (Configuration.RequiresAuthentication)
                            client.Credentials = new NetworkCredential(Configuration.UserName, Configuration.Password);
                        client.DownloadFile(new Uri(remoteUrl), localImagePath);
                    }
                }
                catch (WebException)
                {
                    Plugin.Logger.Info("Could not download logo for Channel: {0}", channel.Title);
                    return null;
                }

                if (Configuration.EnableImageProcessing)
                {
                    ImageHelper.CreateLandscapeImage(localImagePath, localLandscapePath);
                    ImageHelper.CreatePosterImage(localImagePath, localPosterPath);
                    ImageHelper.CreateLogoImage(localImagePath, localLogoPath);

                    return localLogoPath;
                }

                return localImagePath;
            }

            return remoteUrl;
        }
    }
}