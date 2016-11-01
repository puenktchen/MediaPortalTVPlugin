using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal
{
    /// <summary>
    /// Provides MP (v1) integration for Emby
    /// </summary>
    public class MediaPortal1TvService : ILiveTvService
    {
        private static StreamingDetails _currentStreamDetails;
        private bool refreshTimers = false;

        public string HomePageUrl
        {
            get { return "https://github.com/puenktchen/MediaPortalTVPlugin"; }
        }

        public string Name
        {
            get { return "MPExtended (MediaPortal Live TV Service)"; }
        }

    #region General

        public Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            LiveTvServiceStatusInfo result;

            var configurationValidationResult = Plugin.Instance.Configuration.Validate();
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Validate configuration first
            if (!configurationValidationResult.IsValid)
            {
                result = new LiveTvServiceStatusInfo()
                {
                    HasUpdateAvailable = false,
                    Status = LiveTvServiceStatus.Unavailable,
                    StatusMessage = configurationValidationResult.Summary,
                    Tuners = new List<LiveTvTunerInfo>(),
                    Version = String.Format("MediaPortal Plugin: {0} - MPExtended Service: unavailable", version)
                };
            }
            else
            {
                try
                {
                    // Test connections to both the streaming and tv proxy
                    var response = Plugin.StreamingProxy.GetStatusInfo(cancellationToken);
                    response = Plugin.TvProxy.GetStatusInfo(cancellationToken);

                    var activeCards = Plugin.TvProxy.GetActiveCards(cancellationToken);
                    var cards = Plugin.TvProxy.GetTunerCards(cancellationToken).Where(c => c.Enabled).Select(c =>
                    {
                        var activeDetails = activeCards.LastOrDefault(ac => ac.Id == c.Id);
                        var tunerInfo = new LiveTvTunerInfo()
                        {
                            Id = c.Id.ToString(CultureInfo.InvariantCulture),
                            Name = c.Name,
                        };
                        if (activeDetails != null)
                        {
                            tunerInfo.ChannelId = activeDetails.ChannelId.ToString(CultureInfo.InvariantCulture);
                            tunerInfo.ProgramName = Plugin.TvProxy.GetCurrentProgram(cancellationToken, activeDetails.ChannelId);
                            tunerInfo.SourceType = Enum.GetName(typeof(WebCardType), activeDetails.Type);
                            tunerInfo.Clients = new List<string>() { activeDetails.User.Name };
                            tunerInfo.Status =
                            activeDetails.IsRecording ? LiveTvTunerStatus.RecordingTv :
                            activeDetails.IsTunerLocked ? LiveTvTunerStatus.LiveTv : LiveTvTunerStatus.Available;
                        }
                        return tunerInfo;
                    }).ToList();

                    result = new LiveTvServiceStatusInfo()
                    {
                        HasUpdateAvailable = (response.ServiceVersion != "0.6.0.4-Emby") ? true : false,
                        Status = LiveTvServiceStatus.Ok,
                        StatusMessage = String.Format("MediaPortal Plugin: {0} - MPExtended Service: {1}", version, response.ServiceVersion),
                        Tuners = cards,
                        Version = String.Format("MediaPortal Plugin: {0} - MPExtended Service: {1}", version, response.ServiceVersion)
                    };

                }
                catch (Exception ex)
                {
                    Plugin.Logger.Error(ex, "Exception occured getting the MPExtended Service status");

                    result = new LiveTvServiceStatusInfo()
                    {
                        HasUpdateAvailable = false,
                        Status = LiveTvServiceStatus.Unavailable,
                        StatusMessage = "Unable to establish a connection with MPExtended - check your settings",
                        Tuners = new List<LiveTvTunerInfo>(),
                        Version = version
                    };
                }
            }

            return Task.FromResult(result);
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    #endregion

    #region Channels

        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetChannels(cancellationToken));
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetPrograms(channelId, startDateUtc, endDateUtc, cancellationToken));
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    #endregion

    #region Recordings

        public Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetRecordings(cancellationToken));
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            Plugin.TvProxy.DeleteRecording(recordingId, cancellationToken);
            return Task.Delay(0, cancellationToken);
        }

    #endregion

    #region Timers

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program)
        {
            var scheduleDefaults = Plugin.TvProxy.GetScheduleDefaults(cancellationToken);
            var scheduleDayOfWeek = new List<DayOfWeek>();

            if (program != null)
                scheduleDayOfWeek.Add(program.StartDate.ToLocalTime().DayOfWeek);

            return Task.FromResult(new SeriesTimerInfo()
            {
                IsPostPaddingRequired = scheduleDefaults.PostRecordInterval.Ticks > 0,
                IsPrePaddingRequired = scheduleDefaults.PreRecordInterval.Ticks > 0,
                PostPaddingSeconds = (Int32)scheduleDefaults.PostRecordInterval.TotalSeconds,
                PrePaddingSeconds = (Int32)scheduleDefaults.PreRecordInterval.TotalSeconds,
                RecordNewOnly = true,
                RecordAnyChannel = false,
                RecordAnyTime = false,
                Days = scheduleDayOfWeek,
                SkipEpisodesInLibrary = false,
            });
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            if (Plugin.Instance.Configuration.EnableTimerCache)
            {
                var timerCache = MemoryCache.Default;

                if (refreshTimers)
                {
                    timerCache.Remove("timers");
                    refreshTimers = false;
                }

                if (!timerCache.Contains("timers"))
                {
                    Plugin.Logger.Info("Add timers to memory cache");
                    var expiration = DateTimeOffset.UtcNow.AddSeconds(20);
                    var results = Plugin.TvProxy.GetSchedules(cancellationToken);

                    timerCache.Add("timers", Task.FromResult(results), expiration);
                }

                Plugin.Logger.Info("Return timers from memory cache");
                return (Task<IEnumerable<TimerInfo>>)timerCache.Get("timers", null);
            }
            else
            {
                return Task.FromResult(Plugin.TvProxy.GetSchedules(cancellationToken));
            }
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            refreshTimers = true;
            Plugin.TvProxy.CreateSchedule(cancellationToken, info);
            return Task.Delay(0, cancellationToken);
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            refreshTimers = true;
            Plugin.TvProxy.ChangeSchedule(cancellationToken, info);
            return Task.Delay(0, cancellationToken);
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            refreshTimers = true;
            Plugin.TvProxy.DeleteSchedule(cancellationToken, timerId);
            return Task.Delay(0, cancellationToken);
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Plugin.TvProxy.GetSeriesSchedules(cancellationToken));
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            refreshTimers = true;
            Plugin.TvProxy.CreateSeriesSchedule(cancellationToken, info);
            return Task.Delay(0, cancellationToken);
        }

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            refreshTimers = true;
            Plugin.TvProxy.ChangeSeriesSchedule(cancellationToken, info);
            return Task.Delay(0, cancellationToken);;
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            refreshTimers = true;
            Plugin.TvProxy.DeleteSchedule(cancellationToken, timerId);
            return Task.Delay(0, cancellationToken);
        }

    #endregion

    #region Streaming

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _currentStreamDetails = Plugin.StreamingProxy.GetLiveTvStream(cancellationToken, channelId);
            return Task.FromResult(_currentStreamDetails.SourceInfo);
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MediaSourceInfo> GetRecordingStream(string recordingId, string streamId, CancellationToken cancellationToken)
        {
            _currentStreamDetails = Plugin.StreamingProxy.GetRecordingStream(cancellationToken, recordingId, TimeSpan.Zero);
            return Task.FromResult(_currentStreamDetails.SourceInfo);
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            if (_currentStreamDetails.SourceInfo.Id == id)
            {
                Plugin.StreamingProxy.CancelStream(cancellationToken, id);
                return Task.Delay(0);
            }

            throw new Exception(String.Format("Unknown stream id requested for close: {0}", id));
        }

    #endregion

    #region Events

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        public event EventHandler DataSourceChanged;

    #endregion

    }
}