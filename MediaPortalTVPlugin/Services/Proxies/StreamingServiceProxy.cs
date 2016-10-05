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
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding;
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
        private readonly IMediaEncoder _mediaEncoder;

        private String _streamingEndpoint = "StreamingService/stream";

        private const int STREAM_TIMEOUT_DIRECT = 30;
        private const int STREAM_TV_RECORDING_PROVIDER = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingServiceProxy"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="serialiser">The serialiser.</param>
        /// <param name="networkManager">The network manager.</param>
        public StreamingServiceProxy(IHttpClient httpClient, IJsonSerializer serialiser, INetworkManager networkManager, IMediaEncoder mediaEncoder)
            : base(httpClient, serialiser)
        {
            _networkManager = networkManager;
            _mediaEncoder = mediaEncoder;
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
            Plugin.Logger.Info("Streaming setting EnableFFProbe: {0}", Configuration.EnableFFProbe);
            Plugin.Logger.Info("Streaming setting StreamDelay: {0}", Configuration.MediaInfoDelay);
            Plugin.Logger.Info("Streaming setting EnableDirectPlay: {0}", Configuration.EnableDirectPlay);
            Plugin.Logger.Info("Streaming setting LimitDirectPlay to 720p: {0}", Configuration.LimitStreaming);
            
            int mpextendedApiVersion = GetStatusInfo(cancellationToken).ApiVersion;

            var configuration = Plugin.Instance.Configuration;
            var profile = GetTranscoderProfile(cancellationToken, Configuration.StreamingProfileName);
            var identifier = HttpUtility.UrlEncode(String.Format("{0}-{1}-{2:yyyyMMddHHmmss}", webMediaType, itemId, DateTime.UtcNow));
            var url = "streamingURL";
            var mediaInfo = new MediaInfo();

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

            if (configuration.EnableDirectPlay)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                if (Plugin.Instance.Configuration.EnableFFProbe || !String.Equals(profile.Name, "Direct", StringComparison.OrdinalIgnoreCase))
                {
                    mediaInfo = FFProbeStream(cancellationToken, webMediaType, url);
                }
                else
                {
                    mediaInfo = MPEProbeStream(cancellationToken, webMediaType, itemId, profile, identifier);
                }
                
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                Plugin.Logger.Info("Probing RunTime = {0} for {1} stream: {2}", elapsedTime, webMediaType, url);

                if (mediaInfo != null)
                {
                    var defaultVideoStream = mediaInfo.MediaStreams.FirstOrDefault(v => v.Type == Model.Entities.MediaStreamType.Video);
                    var defaultAudioStream = mediaInfo.MediaStreams.FirstOrDefault(a => a.Type == Model.Entities.MediaStreamType.Audio);
                    var defaultSubtitleStream = mediaInfo.MediaStreams.FirstOrDefault(s => s.Type == Model.Entities.MediaStreamType.Subtitle);

                    if (!(configuration.LimitStreaming && defaultVideoStream.Height > 720))
                    {
                        streamingDetails.SourceInfo.Container = mediaInfo.Container;
                        streamingDetails.SourceInfo.Bitrate = mediaInfo.Bitrate;
                        streamingDetails.SourceInfo.MediaStreams = mediaInfo.MediaStreams;

                        if (String.Equals(profile.Name, "Direct", StringComparison.OrdinalIgnoreCase) && defaultVideoStream.Height <= 576)
                        {
                            streamingDetails.SourceInfo.Bitrate = 4000000;
                        }
                        else if (String.Equals(profile.Name, "Direct", StringComparison.OrdinalIgnoreCase) && (defaultVideoStream.Height > 576 || defaultVideoStream.Height <= 720))
                        {
                            streamingDetails.SourceInfo.Bitrate = 7000000;
                        }
                        else if (String.Equals(profile.Name, "Direct", StringComparison.OrdinalIgnoreCase) && defaultVideoStream.Height > 720)
                        {
                            streamingDetails.SourceInfo.Bitrate = 9000000;
                        }

                        if (webMediaType.Equals(WebMediaType.Recording))
                        {
                            var recording = Plugin.TvProxy.GetRecording(cancellationToken, itemId);
                            if (!recording.IsRecording)
                                streamingDetails.SourceInfo.RunTimeTicks = mediaInfo.RunTimeTicks;
                        }
                    }  
                }
            }

            else
            {
                System.Threading.Thread.Sleep(Plugin.Instance.Configuration.MediaInfoDelay.Value);
            }
            
            return streamingDetails;
        }

        /// <summary>
        /// Retrieves media information from an established stream.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <param name="streamIdentifier">The stream identifier.</param>
        /// <returns></returns>
        private WebMediaInfo GetMediaInfoFromStream(CancellationToken cancellationToken, WebMediaType mediaType, String streamIdentifier)
        {
            return GetFromService<WebMediaInfo>(cancellationToken, "GetMediaInfo?type={0}&itemId={1}&provider={2}", mediaType, streamIdentifier, STREAM_TV_RECORDING_PROVIDER);
        }

        private MediaInfo MPEProbeStream(CancellationToken cancellationToken, WebMediaType webMediaType, string itemId, TranscoderProfile profile, String streamIdentifier)
        {
            System.Threading.Thread.Sleep(Plugin.Instance.Configuration.MediaInfoDelay.Value);

            var mediaInfoId = webMediaType == WebMediaType.Recording ? itemId : streamIdentifier;
            var webMediaInfo = GetMediaInfoFromStream(cancellationToken, webMediaType, mediaInfoId);

            List<MediaStream> mediaStreams = new List<MediaStream>();

            for (int i = 0; i < webMediaInfo.VideoStreams.Count; i++)
            {
                var videoStream = new MediaStream()
                {
                    Type = MediaStreamType.Video,
                    Index = (webMediaInfo.VideoStreams[i].StreamOrder.HasValue) ? webMediaInfo.VideoStreams[i].StreamOrder.Value : webMediaInfo.VideoStreams[i].Index,
                    Codec = TranslateMediainfoToEmby.MediaCodec(webMediaInfo.VideoStreams[i].Codec),
                    Profile = "baseline",
                    Level = 40,
                    Height = webMediaInfo.VideoStreams[i].Height,
                    Width = webMediaInfo.VideoStreams[i].Width,
                    BitRate = (webMediaInfo.VideoStreams[i].Height <= 576) ? 4000000 :
                              (webMediaInfo.VideoStreams[i].Height > 576 || webMediaInfo.VideoStreams[i].Height <= 720) ? 7000000 : 9000000, 
                    IsInterlaced = webMediaInfo.VideoStreams[i].Interlaced,
                    ExternalId = webMediaInfo.VideoStreams[i].ID.ToString(CultureInfo.InvariantCulture),
                };
                mediaStreams.Add(videoStream);
            };

            for (int i = 0; i < webMediaInfo.AudioStreams.Count; i++)
            {
                var audioStream = new MediaStream()
                {
                    Type = MediaStreamType.Audio,
                    Index = (webMediaInfo.AudioStreams[i].StreamOrder.HasValue) ? webMediaInfo.AudioStreams[i].StreamOrder.Value : webMediaInfo.AudioStreams[i].Index + webMediaInfo.VideoStreams.Count,
                    Codec = TranslateMediainfoToEmby.MediaCodec(webMediaInfo.AudioStreams[i].Codec),
                    Channels = webMediaInfo.AudioStreams[i].Channels,
                    BitRate = (webMediaInfo.AudioStreams[i].Codec == "MPEG-1 Audio layer 2") ? 192000 :
                              (webMediaInfo.AudioStreams[i].Codec == "AC3+") ? 448000 : 256000,
                    Language = (String.IsNullOrEmpty(webMediaInfo.AudioStreams[i].Language)) ? "unknown" : TranslateMediainfoToEmby.AudioLanguage(webMediaInfo.AudioStreams[i].Language),
                    IsDefault = (webMediaInfo.AudioStreams[i].Index == 0) ? true : false,
                    ExternalId = webMediaInfo.AudioStreams[i].ID.ToString(CultureInfo.InvariantCulture),
                };
                mediaStreams.Add(audioStream);
            }

            foreach (var subtitle in webMediaInfo.SubtitleStreams.Where(s => !String.IsNullOrEmpty(s.Codec) && s.StreamOrder != null))
            {
                var subtitleStream = new MediaStream()
                {
                    Type = MediaStreamType.Subtitle,
                    Index = subtitle.StreamOrder.Value,
                    Codec = TranslateMediainfoToEmby.MediaCodec(subtitle.Codec),
                    Language = (String.IsNullOrEmpty(subtitle.Language)) ? "unknown" : TranslateMediainfoToEmby.AudioLanguage(subtitle.Language),
                };
                mediaStreams.Add(subtitleStream);
            }

            var mediaInfo = new Model.MediaInfo.MediaInfo();

            mediaInfo.Container = TranslateMediainfoToEmby.MediaCodec(webMediaInfo.Container);
            mediaInfo.RunTimeTicks = TimeSpan.FromMilliseconds(webMediaInfo.Duration).Ticks;
            mediaInfo.MediaStreams = mediaStreams;

            return mediaInfo;
        }

        private MediaInfo FFProbeStream(CancellationToken cancellationToken, WebMediaType webMediaType, String probeUrl)
        {
            string ffprobePath = _mediaEncoder.EncoderPath.Replace("ffmpeg.exe", "ffprobe.exe");
            string args = string.Format("-v quiet -print_format json -show_format -show_streams -analyzeduration {0}000 \"{1}\"", Configuration.MediaInfoDelay, probeUrl);

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(ffprobePath);
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();
            p.Start();

            string ffprobeOutput = p.StandardOutput.ReadToEnd().Replace("\r\n", "\n");
            p.WaitForExit();

            FFProbeMediaInfo ffprobeMediaInfo = Serialiser.DeserializeFromString<FFProbeMediaInfo>(ffprobeOutput);

            var mediaInfo = new Model.MediaInfo.MediaInfo();
            var internalStreams = ffprobeMediaInfo.streams ?? new FFProbeMediaStreamInfo[] { };

            mediaInfo.Container = ffprobeMediaInfo.format.format_name;
            mediaInfo.Bitrate = ffprobeMediaInfo.format.bit_rate;
            mediaInfo.RunTimeTicks = (!string.IsNullOrEmpty(ffprobeMediaInfo.format.duration)) ? TimeSpan.FromSeconds(double.Parse(ffprobeMediaInfo.format.duration, CultureInfo.InvariantCulture)).Ticks : 9980000;
            mediaInfo.MediaStreams = internalStreams.Select(s => FFProbeHelper.GetMediaStream(s, ffprobeMediaInfo.format)).Where(i => i != null).ToList();

            return mediaInfo;
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