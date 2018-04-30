using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal
{
    public class RecordingsChannel : IChannel, IHasCacheKey, ISupportsDelete, ISupportsLatestMedia, ISupportsMediaProbe, IHasFolderAttributes
    {
        public ILiveTvManager _liveTvManager;

        private readonly Configuration.PluginConfiguration _pluginConfiguration;

        public RecordingsChannel(ILiveTvManager liveTvManager, Configuration.PluginConfiguration pluginConfiguration)
        {
            _pluginConfiguration = pluginConfiguration;

            if (_pluginConfiguration.EnableRecordingImport)
            {
                _liveTvManager = liveTvManager;
            }
        }

        public string Name
        {
            get
            {
                return "MediaPortal Recordings";
            }
        }

        public string Description
        {
            get
            {
                return "MediaPortal Recordings";
            }
        }

        public string[] Attributes
        {
            get
            {
                return new[] { "Recordings" };
            }
        }

        public string DataVersion
        {
            get
            {
                return "1";
            }
        }

        public string HomePageUrl
        {
            get { return "https://github.com/puenktchen/MediaPortalTVPlugin"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string GetCacheKey(string userId)
        {
            var now = DateTime.UtcNow;

            var values = new List<string>();

            values.Add(now.DayOfYear.ToString(CultureInfo.InvariantCulture));
            values.Add(now.Hour.ToString(CultureInfo.InvariantCulture));

            double minute = now.Minute;
            minute /= 5;

            values.Add(Math.Floor(minute).ToString(CultureInfo.InvariantCulture));

            values.Add(GetService().LastRecordingChange.Ticks.ToString(CultureInfo.InvariantCulture));

            return string.Join("-", values.ToArray());
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Movie,
                    ChannelMediaContentType.Episode,
                    ChannelMediaContentType.Clip
                },
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Audio,
                    ChannelMediaType.Video
                },
                SupportsContentDownloading = true,
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            if (type == ImageType.Primary)
            {
                return Task.FromResult(new DynamicImageResponse
                {
                    Path = ChannelFolderImage("MediaPortal"),
                    Protocol = MediaProtocol.File,
                    HasImage = true
                });
            }

            return Task.FromResult(new DynamicImageResponse
            {
                HasImage = false
            });
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Screenshot,
                ImageType.Thumb,
                ImageType.Primary
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        private MediaPortal1TvService GetService()
        {
            return _liveTvManager.Services.OfType<MediaPortal1TvService>().First();
        }

        public bool CanDelete(BaseItem item)
        {
            return !item.IsFolder;
        }

        public Task DeleteItem(string id, CancellationToken cancellationToken)
        {
            return GetService().DeleteRecordingAsync(id, cancellationToken);
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        { 
            var result = await GetChannelItems(new InternalChannelItemQuery(), i => true, cancellationToken).ConfigureAwait(false); 
            return result.Items.OrderByDescending(i => i.DateModified); 
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.FolderId))
            {
                var recordingGroups = GetRecordingGroups(query, cancellationToken);

                if (recordingGroups.Result.Items.Count == 0)
                {
                    return GetRecordingNameGroups(query, i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries, cancellationToken);
                }

                return recordingGroups;
            }

            if (string.Equals(query.FolderId, "tvshows", StringComparison.OrdinalIgnoreCase))
            {
                return GetRecordingSeriesGroups(query, cancellationToken);
            }

            if (query.FolderId.StartsWith("series_", StringComparison.OrdinalIgnoreCase))
            {
                var hash = query.FolderId.Split('_')[1];
                return GetChannelItems(query, i => i.IsSeries && string.Equals(i.Name.GetMD5().ToString("N"), hash, StringComparison.Ordinal), cancellationToken);
            }

            //// Optional Season Folders ////
            //if (query.FolderId.StartsWith("series_", StringComparison.OrdinalIgnoreCase))
            //{
            //    var hash = query.FolderId.Split('_')[1];
            //    return GetRecordingSeasonGroups(query, i => i.IsSeries && string.Equals(i.Name.GetMD5().ToString("N"), hash, StringComparison.Ordinal), cancellationToken);
            //}

            //if (query.FolderId.StartsWith("season_", StringComparison.OrdinalIgnoreCase))
            //{
            //    Plugin.Logger.Info("QUERY FOLDER ID SEASON: {0}", query.FolderId);
            //    var name = query.FolderId.Split('_')[2];
            //    var hash = query.FolderId.Split('_')[1];

            //    var output = GetChannelItems(query, i => i.IsSeries && string.Equals(i.Name, name) && string.Equals(i.SeasonNumber.ToString().GetMD5().ToString("N"), hash, StringComparison.Ordinal), cancellationToken);
            //    foreach (var item in output.Result.Items)
            //    {
            //        Plugin.Logger.Info("CHANNEL Name {0}; Episode {1}", item.SeriesName, item.Name);
            //    }

            //    return output;
            //}

            if (string.Equals(query.FolderId, "movies", StringComparison.OrdinalIgnoreCase))
            {
                return GetChannelItems(query, i => i.IsMovie, cancellationToken);
            }

            if (string.Equals(query.FolderId, "kids", StringComparison.OrdinalIgnoreCase))
            {
                return GetRecordingNameGroups(query, i => i.IsKids, cancellationToken);
            }

            if (string.Equals(query.FolderId, "news", StringComparison.OrdinalIgnoreCase))
            {
                return GetRecordingNameGroups(query, i => i.IsNews, cancellationToken);
            }

            if (string.Equals(query.FolderId, "sports", StringComparison.OrdinalIgnoreCase))
            {
                return GetRecordingNameGroups(query, i => i.IsSports, cancellationToken);
            }

            if (string.Equals(query.FolderId, "live", StringComparison.OrdinalIgnoreCase))
            {
                return GetRecordingNameGroups(query, i => i.IsLive, cancellationToken);
            }

            if (string.Equals(query.FolderId, "others", StringComparison.OrdinalIgnoreCase))
            {
                return GetRecordingNameGroups(query, i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries, cancellationToken);
            }

            if (query.FolderId.StartsWith("name_", StringComparison.OrdinalIgnoreCase))
            {
                var hash = query.FolderId.Split('_')[1];
                return GetChannelItems(query, i => i.Name != null && string.Equals(i.Name.GetMD5().ToString("N"), hash, StringComparison.Ordinal), cancellationToken);
            }

            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>()
            };

            return Task.FromResult(result);
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, Func<MyRecordingInfo, bool> filter, CancellationToken cancellationToken)
        {
            var service = GetService();
            var allRecordings = await service.GetAllRecordingsAsync(cancellationToken).ConfigureAwait(false);

            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>()
            };

            result.Items.AddRange(allRecordings.Where(filter).Select(ConvertToChannelItem));

            return result;
        }

        private async Task<ChannelItemResult> GetRecordingGroups(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var service = GetService();

            var allRecordings = await service.GetAllRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>(),
            };

            //var latest = allRecordings.OrderByDescending(i => i.StartDate).Take(10);
            //if (latest != null)
            //{
            //    result.Items.AddRange(latest.Select(ConvertToChannelItem));
            //}

            var series = allRecordings.FirstOrDefault(i => i.IsSeries);
            if (series != null)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "TV Shows",
                    FolderType = ChannelFolderType.Container,
                    Id = "tvshows",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("TV Shows")
                });
            }

            var movies = allRecordings.FirstOrDefault(i => i.IsMovie);
            if (movies != null)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "Movies",
                    FolderType = ChannelFolderType.Container,
                    Id = "movies",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("Movies")
                });
            }

            var kids = allRecordings.FirstOrDefault(i => i.IsKids);
            if (kids != null)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "Kids",
                    FolderType = ChannelFolderType.Container,
                    Id = "kids",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("Kids")
                });
            }

            var news = allRecordings.FirstOrDefault(i => i.IsNews);
            if (news != null)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "News & Documentary",
                    FolderType = ChannelFolderType.Container,
                    Id = "news",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("News & Documentary")
                });
            }

            var sports = allRecordings.FirstOrDefault(i => i.IsSports);
            if (sports != null)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "Sports",
                    FolderType = ChannelFolderType.Container,
                    Id = "sports",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("Sports")
                });
            }

            var live = allRecordings.FirstOrDefault(i => i.IsLive);
            if (live != null)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "Live Shows",
                    FolderType = ChannelFolderType.Container,
                    Id = "live",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("Live Shows")
                });
            }

            var other = allRecordings.FirstOrDefault(i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries);
            if (other != null && result.Items.Count > 0)
            {
                result.Items.Add(new ChannelItemInfo
                {
                    Name = "Other Shows",
                    FolderType = ChannelFolderType.Container,
                    Id = "others",
                    Type = ChannelItemType.Folder,
                    ImageUrl = ChannelFolderImage("Other Shows")
                });
            }

            return result;
        }

        private async Task<ChannelItemResult> GetRecordingSeriesGroups(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var service = GetService();

            var allRecordings = await service.GetAllRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>(),
            };

            var series = allRecordings
                .Where(i => i.IsSeries)
                .ToLookup(i => i.Name, StringComparer.OrdinalIgnoreCase);

            result.Items.AddRange(series.OrderBy(i => i.Key).Select(i => new ChannelItemInfo
            {
                Name = i.Key,
                FolderType = ChannelFolderType.Container,
                Id = "series_" + i.Key.GetMD5().ToString("N"),
                Type = ChannelItemType.Folder,
                ImageUrl = File.Exists(Path.Combine(pluginPath, "recordingposters", String.Join("", i.Key.Split(Path.GetInvalidFileNameChars())) + ".jpg")) ?
                           Path.Combine(pluginPath, "recordingposters", String.Join("", i.Key.Split(Path.GetInvalidFileNameChars())) + ".jpg") : String.Empty,
            }));

            return result;
        }

        private async Task<ChannelItemResult> GetRecordingSeasonGroups(InternalChannelItemQuery query, Func<MyRecordingInfo, bool> filter, CancellationToken cancellationToken)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var service = GetService();

            var allRecordings = await service.GetAllRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>(),
            };

            var season = allRecordings.Where(filter)
                .GroupBy(i => i.SeasonNumber, i => i.Name, (key, g) => new { SeasonNumber = key, Name = g.ToList() });

            result.Items.AddRange(season.OrderBy(i => i.SeasonNumber).Select(i => new ChannelItemInfo
            {
                Name = "Season " + i.SeasonNumber,
                FolderType = ChannelFolderType.Container,
                Id = "season_" + i.SeasonNumber.ToString().GetMD5().ToString("N") + "_" + i.Name.First().ToString(),
                Type = ChannelItemType.Folder,
                ParentIndexNumber = i.SeasonNumber,
            }));

            result.Items.OrderBy(i => i.Name);

            return result;
        }

        private async Task<ChannelItemResult> GetRecordingNameGroups(InternalChannelItemQuery query, Func<MyRecordingInfo, bool> filter, CancellationToken cancellationToken)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var service = GetService();

            var allRecordings = await service.GetAllRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>(),
            };

            var doublenames = allRecordings.Where(filter)
                .GroupBy(i => i.Name).Where(i => i.Count() > 1).Select(i => i.Key)
                .ToLookup(i => i, StringComparer.OrdinalIgnoreCase);

            result.Items.AddRange(doublenames.OrderBy(i => i.Key).Select(i => new ChannelItemInfo
            {
                Name = i.Key,
                FolderType = ChannelFolderType.Container,
                Id = "name_" + i.Key.GetMD5().ToString("N"),
                Type = ChannelItemType.Folder,
                ImageUrl = File.Exists(Path.Combine(pluginPath, "recordingposters", String.Join("", i.Key.Split(Path.GetInvalidFileNameChars())) + ".jpg")) ?
                           Path.Combine(pluginPath, "recordingposters", String.Join("", i.Key.Split(Path.GetInvalidFileNameChars())) + ".jpg") : String.Empty,
            }));

            var singlenames = allRecordings.Where(filter)
                .GroupBy(i => i.Name).Where(c => c.Count() == 1).Select(g => g.First());

            result.Items.AddRange(singlenames.Select(ConvertToChannelItem));

            result.Items.OrderBy(i => i.Name);

            return result;
        }

        private ChannelItemInfo ConvertToChannelItem(MyRecordingInfo item)
        {
            var config = Plugin.Instance.Configuration;

            var channelItem = new ChannelItemInfo
            {
                Id = item.Id,
                Name = !string.IsNullOrEmpty(item.EpisodeTitle) && (item.IsSeries || item.EpisodeNumber.HasValue && !item.IsMovie) ? item.EpisodeTitle : item.Name,
                SeriesName = !string.IsNullOrEmpty(item.EpisodeTitle) && (item.IsSeries || item.EpisodeNumber.HasValue && !item.IsMovie) ? item.Name : null,
                OriginalTitle = !string.IsNullOrEmpty(item.EpisodeTitle) && item.IsMovie ? item.EpisodeTitle : null,
                IndexNumber = item.EpisodeNumber,
                ParentIndexNumber = item.SeasonNumber,
                ProductionYear = item.Year,
                Overview = item.Overview,
                Genres = item.Genres,
                ImageUrl = (item.IsMovie && !string.IsNullOrEmpty(item.TmdbPoster)) ? item.TmdbPoster : item.ImageUrl,
                DateCreated = item.StartDate,
                DateModified = item.EndDate,

                Type = ChannelItemType.Media,
                ContentType = item.IsMovie ? ChannelMediaContentType.Movie : (item.IsSeries || item.EpisodeNumber != null ? ChannelMediaContentType.Episode : ChannelMediaContentType.Clip),
                MediaType = item.ChannelType == ChannelType.TV ? ChannelMediaType.Video : ChannelMediaType.Audio,
                IsLiveStream = item.Status == RecordingStatus.InProgress,
                Etag = item.Status.ToString(),

                MediaSources = new List<MediaSourceInfo>
                {
                    new MediaSourceInfo
                    {
                        Path = config.EnableDirectAccess && item.Status != RecordingStatus.InProgress ? item.Path : Plugin.StreamingProxy.GetRecordingStream(item.Id),
                        Protocol = item.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? MediaProtocol.Http : MediaProtocol.File,
                        Id = item.Id,
                        SupportsProbing = item.Status == RecordingStatus.InProgress ? false : true,
                        IsInfiniteStream = item.Status == RecordingStatus.InProgress ? true : false,
                        ReadAtNativeFramerate = item.Status == RecordingStatus.InProgress ? true : false,
                        RunTimeTicks = (item.EndDate - item.StartDate).Ticks,

                        RequiredHttpHeaders = config.RequiresAuthentication ?
                            new Dictionary<string, string> {{ "Authentication", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(String.Format("{0}:{1}", config.UserName, config.Password))) }} :
                            new Dictionary<string, string> {},
                    }
                }
            };

            return channelItem;
        }

        Assembly _assembly;
        Stream _imageStream;

        private String ChannelFolderImage(string name)
        {
            var plugin = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var localPoster = Path.Combine(plugin, "recordingposters", name + "-thumb.png");

            if (!Directory.Exists(Path.Combine(plugin, "recordingposters")))
            {
                Directory.CreateDirectory(Path.Combine(plugin, "recordingposters"));
            }

            _assembly = Assembly.GetExecutingAssembly();
            _imageStream = _assembly.GetManifestResourceStream("MediaBrowser.Plugins.MediaPortal.Images." + name + "-thumb.png");

            using (var fileStream = File.Create(localPoster))
            {
                _imageStream.Seek(0, SeekOrigin.Begin);
                _imageStream.CopyTo(fileStream);
            }

            return localPoster;
        }
    }
}

