using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class TmdbLookup
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        public TmdbLookup(IHttpClient httpClient, IJsonSerializer json, IServerConfigurationManager serverConfigurationManager)
        {
            _httpClient = httpClient;
            _json = json;
            _serverConfigurationManager = serverConfigurationManager;
        }

        public void GetTmdbPoster(CancellationToken cancellationToken, MyRecordingInfo recording)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var localPoster = Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + ".jpg");
            var localPosterMissing = Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + " [missing].jpg");
            
            if (!Directory.Exists(Path.Combine(pluginPath, "recordingposters")))
            {
                Directory.CreateDirectory(Path.Combine(pluginPath, "recordingposters"));
            }

            if (recording.IsMovie && !(File.Exists(localPoster) || File.Exists(localPosterMissing)))
            {
                try
                {
                    using (var tmdbMovieSearch = _httpClient.Get(new HttpRequestOptions()
                    {
                        Url = $"https://api.themoviedb.org/3/search/movie?api_key=9dbbec013a2d32baf38ccc58006cd991&query={recording.Name}" + $"&language={_serverConfigurationManager.Configuration.PreferredMetadataLanguage}",
                        CancellationToken = cancellationToken,
                        BufferContent = false,
                        EnableDefaultUserAgent = true,
                        AcceptHeader = "application/json",
                        EnableHttpCompression = true,
                        DecompressionMethod = CompressionMethod.Gzip
                    }).Result)
                    {
                        var movie = _json.DeserializeFromStream<TmdbMovieSearch>(tmdbMovieSearch);

                        if (movie.total_results > 0)
                        {
                            TmdbMovieResult tmdbMovieResult = movie.results.Find(x => x.title.Equals(recording.Name) || x.original_title.Contains(recording.EpisodeTitle)) ?? movie.results.First();

                            if (recording.Year.HasValue)
                            {
                                tmdbMovieResult = movie.results.Find(x => x.release_date.StartsWith(recording.Year.Value.ToString())) ?? movie.results.First();
                            }

                            var moviePoster = tmdbMovieResult.poster_path;

                            if (!String.IsNullOrEmpty(moviePoster))
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.DownloadFile(new Uri($"https://image.tmdb.org/t/p/w500{moviePoster}"), localPoster);
                                }
                            }
                            else
                            {
                                File.Create(localPosterMissing);
                            }
                        }
                        else
                        {
                            File.Create(localPosterMissing);
                        }
                    }
                }
                catch (WebException)
                {
                    Plugin.Logger.Info("Could not download poster for Movie Recording: {0}", recording.Name);
                }
            }

            if (recording.SeasonNumber.HasValue && recording.EpisodeNumber.HasValue && !(File.Exists(localPoster) || File.Exists(localPosterMissing)))
            {
                try
                {
                    using (var tmdbTvSearch = _httpClient.Get(new HttpRequestOptions()
                    {
                        Url = $"https://api.themoviedb.org/3/search/tv?api_key=9dbbec013a2d32baf38ccc58006cd991&query={recording.Name}" + $"&language={_serverConfigurationManager.Configuration.PreferredMetadataLanguage}",
                        CancellationToken = cancellationToken,
                        BufferContent = false,
                        EnableDefaultUserAgent = true,
                        AcceptHeader = "application/json",
                        EnableHttpCompression = true,
                        DecompressionMethod = CompressionMethod.Gzip
                    }).Result)
                    {
                        //var posterUrl = string.Empty;

                        //for (int i = 0; i < tvshow.results.Count; i++)
                        //{
                        //    TmdbTvResult tmdbTvResult = tvshow.results.ElementAt(i);
                        //
                        //    using (var tmdbEpisode = await _httpClient.Get(new HttpRequestOptions()
                        //    {
                        //        Url = $"https://api.themoviedb.org/3/tv/{tmdbTvResult.id}/season/{recording.SeasonNumber}/episode/{recording.EpisodeNumber}?api_key=9dbbec013a2d32baf38ccc58006cd991" + $"&language={_serverConfigurationManager.Configuration.UICulture}",
                        //        CancellationToken = cancellationToken,
                        //        BufferContent = false,
                        //        EnableDefaultUserAgent = true,
                        //        AcceptHeader = "application/json",
                        //        EnableHttpCompression = true,
                        //        DecompressionMethod = CompressionMethod.Gzip
                        //    }).ConfigureAwait(true))
                        //    {
                        //        var episode = _json.DeserializeFromStream<TmdbEpisodeResult>(tmdbEpisode);
                        //
                        //        if (episode.name == recording.EpisodeTitle)
                        //        {
                        //            posterUrl = $"https://image.tmdb.org/t/p/original{tmdbTvResult.poster_path}";
                        //            break;
                        //        }
                        //    }
                        //
                        //    Thread.Sleep(400);
                        //}

                        var tvshow = _json.DeserializeFromStream<TmdbTvSearch>(tmdbTvSearch);

                        if (tvshow.total_results > 0)
                        {
                            TmdbTvResult tmdbTvResult = tvshow.results.Find(x => x.name.Equals(recording.Name)) ?? tvshow.results.First();
                            var tvPoster = tmdbTvResult.poster_path;

                            if (!String.IsNullOrEmpty(tvPoster))
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.DownloadFile(new Uri($"https://image.tmdb.org/t/p/w500{tvPoster}"), localPoster);
                                }
                            }
                            else
                            {
                                File.Create(localPosterMissing);
                            }
                        }
                        else
                        {
                            File.Create(localPosterMissing);
                        }
                    }
                }
                catch (WebException)
                {
                    Plugin.Logger.Info("Could not download poster for TV Show Recording: {0}", recording.Name);
                }
            }
        }

        private class TmdbMovieSearch
        {
            public int total_results { get; set; }
            public List<TmdbMovieResult> results { get; set; }
        }

        private class TmdbMovieResult
        {
            public int id { get; set; }
            public string title { get; set; }
            public string original_title { get; set; }
            public string original_language { get; set; }
            public string release_date { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        private class TmdbTvSearch
        {
            public int total_results { get; set; }
            public List<TmdbTvResult> results { get; set; }
        }

        private class TmdbTvResult
        {
            public int id { get; set; }
            public string name { get; set; }
            public string original_title { get; set; }
            public string original_language { get; set; }
            public string first_air_date { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        private class TmdbEpisodeResult
        {
            public string name { get; set; }
        }
    }
}