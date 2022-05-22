using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;

using MediaBrowser.Plugins.MediaPortal.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services
{
    public class StreamingService : ProxyService
    {
        public StreamingService(IHttpClient httpClient, IJsonSerializer serialiser) : base(httpClient, serialiser)
        {
        }

        protected override string EndPointSuffix
        {
            get { return "StreamingService/json"; }
        }

        public async Task<List<TranscoderProfile>> GetTranscoderProfiles(string url, MediaPortalOptions configuration, CancellationToken cancellationToken)
        {
            return await GetFromServiceAsync<List<TranscoderProfile>>(url, configuration, cancellationToken, "GetTranscoderProfiles");
        }

        public async Task<MediaSourceInfo> GetLiveTvStream(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, ChannelType channelType, string channelId, string mediaSourceId)
        {
            var mediaSourceInfo = new MediaSourceInfo();

            var authorized = await GetFromServiceAsync<WebBoolResult>(url, configuration, cancellationToken, "AuthorizeStreaming").ConfigureAwait(false);

            if (authorized.Result)
            {
                mediaSourceInfo.Id = mediaSourceId;
                mediaSourceInfo.Protocol = MediaProtocol.Http;
                mediaSourceInfo.RequiresOpening = true;
                mediaSourceInfo.RequiresClosing = true;
                mediaSourceInfo.SupportsDirectPlay = false;
                mediaSourceInfo.SupportsDirectStream = true;
                mediaSourceInfo.SupportsTranscoding = true;
                mediaSourceInfo.Path = GetUrl(url, configuration, "StreamingService/stream", "DoStream?type={0}&provider=0&itemId={1}&clientDescription={2}&profileName={3}&startPosition=0&idleTimeout=30&identifier={4}",
                    channelType == ChannelType.TV ? WebMediaType.TV : WebMediaType.Radio,
                    channelId,
                    mediaSourceId,
                    configuration.TranscoderProfile,
                    mediaSourceId);

                if (!string.IsNullOrEmpty(configuration.UserName))
                {
                    string authInfo = string.Format("{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

                    mediaSourceInfo.RequiredHttpHeaders = new Dictionary<string, string> { { "Authentication", "Basic " + authInfo } };
                }
            }

            return mediaSourceInfo;
        }

        public Task CancelStream(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, string identifier)
        {
            return GetFromServiceAsync<WebBoolResult>(url, configuration, cancellationToken, "FinishStream?identifier={0}", identifier);
        }

        public string GetChannelLogo(string url, MediaPortalOptions configuration, bool isTvChannel, string channelId)
        {
            var cachePath = Path.Combine(Plugin.ConfigurationManager.CommonApplicationPaths.CachePath, "mediaportal");
            var remoteUrl = GetUrl(url, configuration, "StreamingService/stream", "GetArtwork?id={0}&artworktype=5&offset=0&mediatype={1}", channelId, isTvChannel ? WebMediaType.TV : WebMediaType.Radio);

            var localImagePath = string.Empty;
            var localLogoPath = Path.Combine(cachePath, channelId + "-logo.png");
            var localLandscapePath = Path.Combine(cachePath, channelId + "-landscape.png");
            var localPosterPath = Path.Combine(cachePath, channelId + "-poster.png");

            if (Directory.Exists(cachePath))
            {
                localImagePath = Directory.EnumerateFiles(cachePath, string.Format(@"{0}.*", channelId), SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (localImagePath != null)
                {
                    return localImagePath;
                }
            }
            else
            {
                Directory.CreateDirectory(cachePath);
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(configuration.UserName))
                    {
                        var authInfo = string.Format(@"{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                        var byteArray = Encoding.ASCII.GetBytes(authInfo);
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }

                    var response = client.GetAsync(remoteUrl).Result;
                    
                    var filetype = response.Content.Headers.ContentType.MediaType;

                    if (!filetype.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    var fileExtension = filetype.Split('/')[1];

                    localImagePath = Path.Combine(cachePath, channelId + "." + fileExtension);

                    var imageArray = response.Content.ReadAsByteArrayAsync().Result;

                    File.WriteAllBytes(localImagePath, imageArray);
                }                
            }
            catch (Exception)
            {
                return null;
            }

            Plugin.ImageCreator.CreateLogoImage(localImagePath, localLogoPath);
            Plugin.ImageCreator.CreateLandscapeImage(localImagePath, localLandscapePath);
            Plugin.ImageCreator.CreatePosterImage(localImagePath, localPosterPath);
            
            if (!File.Exists(localImagePath))
            {
                return null;
            }

            return localImagePath;
        }
    }
}