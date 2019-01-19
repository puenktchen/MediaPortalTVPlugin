using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.MediaPortal.Entities;
using MediaBrowser.Plugins.MediaPortal.Helpers;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services.Proxies
{
    /// <summary>
    /// Provides access to the MP tv service functionality
    /// </summary>
    public class TvServiceProxy : ProxyBase
    {
        private readonly StreamingServiceProxy _wssProxy;
        private TmdbLookup _tmdbLookup;

        public TvServiceProxy(IHttpClient httpClient, IJsonSerializer serialiser, StreamingServiceProxy wssProxy, TmdbLookup tmdbLookup)
            : base(httpClient, serialiser)
        {
            _wssProxy = wssProxy;
            _tmdbLookup = tmdbLookup;
        }

        protected override string EndPointSuffix
        {
            get { return "TVAccessService/json"; }
        }

        #region Get Methods

        public ServiceDescription GetStatusInfo(CancellationToken cancellationToken)
        {
            return GetFromService<ServiceDescription>(cancellationToken, "GetServiceDescription");
        }

        public List<TunerCard> GetTunerCards(CancellationToken cancellationToken)
        {
            return GetFromService<List<TunerCard>>(cancellationToken, "GetCards");
        }

        public List<ActiveTunerCard> GetActiveCards(CancellationToken cancellationToken)
        {
            return GetFromService<List<ActiveTunerCard>>(cancellationToken, "GetActiveCards");
        }

        public List<ChannelGroup> GetTvChannelGroups(CancellationToken cancellationToken)
        {
            return GetFromService<List<ChannelGroup>>(cancellationToken, "GetGroups").OrderBy(g => g.SortOrder).ToList();
        }

        public List<ChannelGroup> GetRadioChannelGroups(CancellationToken cancellationToken)
        {
            return GetFromService<List<ChannelGroup>>(cancellationToken, "GetRadioGroups").OrderBy(g => g.SortOrder).ToList();
        }

        public IEnumerable<ChannelInfo> GetChannels(CancellationToken cancellationToken)
        {
            var tvChannels = GetFromService<List<Channel>>(cancellationToken, "GetChannelsDetailed?groupId={0}", Configuration.TvChannelGroup);
            var radioChannels = GetFromService<List<Channel>>(cancellationToken, "GetRadioChannelsDetailed?groupId={0}", Configuration.RadioChannelGroup);

            IEnumerable<Channel> channels = tvChannels.Concat(radioChannels);

            Plugin.Logger.Info("Found overall channels: {0}", channels.Where(c => c.VisibleInGuide).Count());
            return channels.Where(c => c.VisibleInGuide).Select((c, index) =>
            {
                var channel = new ChannelInfo()
                {
                    Id = c.Id,
                    Name = c.Title,
                    Number = (index + 1).ToString("D1", CultureInfo.InvariantCulture),
                    ChannelType = ChannelType.TV,
                    ImageUrl = _wssProxy.GetChannelLogo(c),
                };

                return channel;
            });
        }

        public string GetCurrentProgram(CancellationToken cancellationToken, int channelId)
        {
            return GetFromService<Program>(cancellationToken, "GetCurrentProgramOnChannel?channelId={0}", channelId).Title;
        }

        private Program GetProgram(CancellationToken cancellationToken, String programId)
        {
            return GetFromService<Program>(cancellationToken, "GetProgramDetailedById?programId={0}", programId);
        }

        public IEnumerable<ProgramInfo> GetPrograms(string channelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc, CancellationToken cancellationToken)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var response = GetFromService<List<Program>>(
                cancellationToken,
                "GetProgramsDetailedForChannel?channelId={0}&startTime={1}&endTime={2}",
                channelId,
                startDateUtc.ToLocalTime().ToUrlDate(),
                endDateUtc.ToLocalTime().ToUrlDate());

            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);

            Plugin.Logger.Info("Found programs: {0}  for channel id: {1}", response.Count(), channelId);
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
                    Genres = new List<String>(),
                };

                //program.IsSeries = true; //is set by genreMapper
                if (!String.IsNullOrEmpty(p.Genre))
                {
                    program.Genres.Add(p.Genre);
                    genreMapper.PopulateProgramGenres(program);
                }

                if (program.IsSeries && p.Title != p.EpisodeName)
                {
                    program.EpisodeTitle = p.EpisodeName;
                }

                if (Configuration.ProgramImages)
                {
                    if (File.Exists(Path.Combine(pluginPath, "channellogos", channelId + ".png")))
                    {
                        program.ImageUrl = Path.Combine(pluginPath, "channellogos", channelId + ".png");

                        if (Configuration.EnableImageProcessing)
                        {
                            program.ImageUrl = Path.Combine(pluginPath, "channellogos", channelId + "-poster.png"); ;
                            program.ThumbImageUrl = Path.Combine(pluginPath, "channellogos", channelId + "-landscape.png");
                        }
                    }
                }

                return program;
            });
        }

        public Recording GetRecording(CancellationToken cancellationToken, String recordingId)
        {
            return GetFromService<Recording>(cancellationToken, "GetRecordingById?id={0}", recordingId);
        }

        public IEnumerable<MyRecordingInfo> GetRecordings(CancellationToken cancellationToken)
        {
            var pluginPath = Plugin.Instance.ConfigurationFilePath.Remove(Plugin.Instance.ConfigurationFilePath.Length - 4);
            var localPath = String.Format("{0}", Configuration.LocalFilePath);
            var remotePath = String.Format("{0}", Configuration.RemoteFilePath);
            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);
            var lastName = string.Empty;

            var recordings = GetFromService<List<Recording>>(cancellationToken, "GetRecordings").Select(r =>
            {
                var recording = new MyRecordingInfo()
                {
                    Id = r.Id,
                    Name = r.Title,
                    EpisodeTitle = r.EpisodeName,
                    EpisodeNumber = r.EpisodeNumber,
                    SeasonNumber = r.SeasonNumber,
                    Overview = r.Description,
                    Year = r.Year,
                    Genres = new List<String>(),
                    TimerId = r.ScheduleId.ToString(CultureInfo.InvariantCulture),
                    ChannelId = r.ChannelId.ToString(CultureInfo.InvariantCulture),
                    ChannelName = r.ChannelName,
                    ChannelType = ChannelType.TV,
                    StartDate = r.StartTime,
                    EndDate = r.EndTime,
                    Status = (r.IsRecording) ? RecordingStatus.InProgress : RecordingStatus.Completed,
                    Path = r.FileName,
                };

                //recording.IsSeries = true; //is set by genreMapper
                if (!String.IsNullOrEmpty(r.Genre))
                {
                    recording.Genres.Add(r.Genre);
                    genreMapper.PopulateRecordingGenres(recording);
                }

                if (recording.IsMovie)
                {
                    recording.Name = r.MovieName;
                }

                if (r.IsRecording)
                {
                    var schedule = GetSchedule(cancellationToken, r.ScheduleId.ToString());
                    recording.EndDate = schedule.EndTime + TimeSpan.FromMinutes(schedule.PostRecordInterval);
                }

                if (!r.IsRecording)
                {
                    recording.ImageUrl = _wssProxy.GetRecordingImage(r.Id);
                }

                if (Configuration.RequiresPathSubstitution)
                {
                    recording.Path = r.FileName.Replace(localPath, remotePath);
                }

                if (Configuration.EnableTmdbLookup)
                {
                    if (recording.Name != lastName)
                    {
                        _tmdbLookup.GetTmdbPoster(cancellationToken, recording);
                    }
                    lastName = recording.Name;
                }

                if (File.Exists(Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + ".jpg")))
                {
                    recording.TmdbPoster = Path.Combine(pluginPath, "recordingposters", String.Join("", recording.Name.Split(Path.GetInvalidFileNameChars())) + ".jpg");
                }

                return recording;
            }).ToList();

            Plugin.Logger.Info("Found recordings: {0} ", recordings.Count());
            return recordings;
        }

        public List<Schedule> GetCurrentSchedules(CancellationToken cancellationToken)
        {
            return GetFromService<List<Schedule>>(cancellationToken, "GetSchedules");
        }

        public Schedule GetSchedule(CancellationToken cancellationToken, String Id)
        {
            return GetFromService<Schedule>(cancellationToken, "GetScheduleById?scheduleId={0}", Id);
        }

        private bool refreshTimers { get; set; }
        private object _timerLock = new object();
        private List<TimerInfo> _timerCache = null;

        public void RefreshSchedules(CancellationToken cancellationToken)
        {
            refreshTimers = true;
            refreshSeriesTimers = true;

            Plugin.Logger.Info("Refreshing onetime schedules now.");

            GetSeriesSchedulesFromMemory(cancellationToken);
            GetSchedulesFromMemory(cancellationToken);
        }

        public IEnumerable<TimerInfo> GetSchedulesFromMemory(CancellationToken cancellationToken)
        {
            lock (_timerLock)
            {
                if (refreshTimers || _timerCache == null)
                {
                    Plugin.Logger.Info("Writing onetime schedules to memory cache");
                    _timerCache = GetSchedules(cancellationToken).ToList();
                    refreshTimers = false;
                }
                Plugin.Logger.Info("Return onetime schedules from memory cache");
                return _timerCache;
            }
        }

        public IEnumerable<TimerInfo> GetSchedules(CancellationToken cancellationToken)
        {
            List<TimerInfo> schedules = new List<TimerInfo>();

            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);

            var backendTimers = GetFromService<List<Schedule>>(cancellationToken, "GetSchedules");

            Plugin.Logger.Info("Found one time schedules: {0}", backendTimers.Where(c => c.ScheduleType == 0).Count());
            Plugin.Logger.Info("Found series schedules: {0}", backendTimers.Where(c => c.ScheduleType > 0).Count());

            foreach (var schedule in backendTimers.Where(s => s.ScheduleType == 0))
            {
                var timerInfo = new TimerInfo();
                timerInfo.Name = schedule.Title;
                timerInfo.Id = schedule.Id.ToString(CultureInfo.InvariantCulture);
                timerInfo.SeriesTimerId = (schedule.ParentScheduleId > 0) ? schedule.ParentScheduleId.ToString(CultureInfo.InvariantCulture) : null;
                timerInfo.ProgramId = schedule.Id.ToString(CultureInfo.InvariantCulture);
                timerInfo.ChannelId = schedule.ChannelId.ToString(CultureInfo.InvariantCulture);
                timerInfo.StartDate = schedule.StartTime;
                timerInfo.EndDate = schedule.EndTime;
                timerInfo.IsPrePaddingRequired = (schedule.PreRecordInterval > 0);
                timerInfo.IsPostPaddingRequired = (schedule.PostRecordInterval > 0);
                timerInfo.PrePaddingSeconds = schedule.PreRecordInterval * 60;
                timerInfo.PostPaddingSeconds = schedule.PostRecordInterval * 60;
                timerInfo.Status = (((schedule.StartTime - TimeSpan.FromMinutes(schedule.PreRecordInterval) < DateTimeOffset.UtcNow)))
                                   && (DateTimeOffset.UtcNow < (schedule.EndTime + TimeSpan.FromMinutes(schedule.PostRecordInterval)))
                                   ? RecordingStatus.InProgress : RecordingStatus.New;

                var programResponse = GetFromService<List<Program>>(cancellationToken, "SearchProgramsDetailed?searchTerm={0}", schedule.Title);
                var program = programResponse.Where(p => (p.StartTime == schedule.StartTime && p.ChannelId == schedule.ChannelId)).FirstOrDefault();
                if (program != null)
                {
                    timerInfo.ProgramId = program.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.Name = (!String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName : program.Title;
                    timerInfo.EpisodeTitle = program.EpisodeName;
                    timerInfo.EpisodeNumber = program.EpisodeNumber;
                    timerInfo.SeasonNumber = program.SeasonNumber;
                    timerInfo.Overview = program.Description;
                    var genres = new List<String>();
                    timerInfo.Status = (program.HasConflict) ? RecordingStatus.ConflictedNotOk : timerInfo.Status;

                    //timerInfo.IsProgramSeries = true; //is set by genreMapper
                    if (!String.IsNullOrEmpty(program.Genre))
                    {
                        genres.Add(program.Genre);
                        genreMapper.PopulateTimerGenres(timerInfo);
                    }

                    timerInfo.Genres = genres.ToArray();

                    Plugin.Logger.Info("One time schedule: \"{0}\"; Channel: {1}; Start Time: {2}; End Time: {3}; Status: {4}", schedule.Title, schedule.ChannelId, schedule.StartTime, schedule.EndTime, timerInfo.Status.ToString());
                }
                else
                {
                    Plugin.Logger.Info("The one time schedule: \"{0}\" does not match any program with start time: {1} on channel: {2}", schedule.Title, schedule.StartTime, schedule.ChannelId);
                }
                schedules.Add(timerInfo);
            }

            foreach (var schedule in backendTimers.Where(s => s.ScheduleType > 0).DistinctBy(s => s.Title))
            {
                var programResponse = GetFromService<List<Program>>(cancellationToken, "SearchProgramsDetailed?searchTerm={0}", schedule.Title);
                Plugin.Logger.Info("Found pending timers: {0}  for series schedule: \"{1}\"", programResponse.Where(p =>
                    (p.IsRecordingSeriesPending || p.IsPartialRecordingSeriesPending || p.IsRecordingSeriesCanceled || p.IsScheduled) && !(p.IsRecordingOnce || p.IsRecordingOncePending)).Count(), schedule.Title);

                foreach (var program in programResponse.Where(p => (p.IsRecordingSeriesPending || p.IsPartialRecordingSeriesPending || p.IsRecordingSeriesCanceled || p.IsScheduled) && !(p.IsRecordingOnce || p.IsRecordingOncePending)))
                {
                    var timerInfo = new TimerInfo();
                    timerInfo.Name = (!String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName : program.Title;
                    timerInfo.Id = program.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.SeriesTimerId = schedule.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.ProgramId = program.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.ChannelId = program.ChannelId.ToString(CultureInfo.InvariantCulture);
                    timerInfo.EpisodeTitle = program.EpisodeName;
                    timerInfo.EpisodeNumber = program.EpisodeNumber;
                    timerInfo.SeasonNumber = program.SeasonNumber;
                    timerInfo.Overview = program.Description;
                    timerInfo.StartDate = program.StartTime;
                    timerInfo.EndDate = program.EndTime;
                    timerInfo.IsPrePaddingRequired = (schedule.PreRecordInterval > 0);
                    timerInfo.IsPostPaddingRequired = (schedule.PostRecordInterval > 0);
                    timerInfo.PrePaddingSeconds = schedule.PreRecordInterval * 60;
                    timerInfo.PostPaddingSeconds = schedule.PostRecordInterval * 60;
                    timerInfo.Status = (program.IsRecordingSeriesCanceled) ? RecordingStatus.Cancelled :
                                       (program.HasConflict) ? RecordingStatus.ConflictedNotOk :
                                       RecordingStatus.New;

                    Plugin.Logger.Info("Seriespart schedule: \"{0}\"; Channel: {1}; Start Time: {2}; End Time: {3}; Status: {4}", program.Title, program.ChannelId, program.StartTime, program.EndTime, timerInfo.Status.ToString());

                    schedules.Add(timerInfo);
                };
            }

            return schedules;
        }

        private bool refreshSeriesTimers { get; set; }
        private object _seriesTimerLock = new object();
        private List<SeriesTimerInfo> _seriesTimerCache = null;

        public IEnumerable<SeriesTimerInfo> GetSeriesSchedulesFromMemory(CancellationToken cancellationToken)
        {
            lock (_seriesTimerLock)
            {
                if (refreshSeriesTimers || _seriesTimerCache == null)
                {
                    Plugin.Logger.Info("Writing series schedules to memory cache");
                    _seriesTimerCache = GetSeriesSchedules(cancellationToken).ToList();
                    refreshSeriesTimers = false;
                }
                Plugin.Logger.Info("Return series schedules from memory cache");
                return _seriesTimerCache;
            }
        }

        public IEnumerable<SeriesTimerInfo> GetSeriesSchedules(CancellationToken cancellationToken)
        {
            var backendTimers = GetFromService<List<Schedule>>(cancellationToken, "GetSchedules");

            var seriesSchedules = backendTimers.Where(t => t.ScheduleType > 0).Select(t =>
            {
                var seriesTimerInfo = new SeriesTimerInfo()
                {
                    Name = t.Title,
                    Id = t.Id.ToString(CultureInfo.InvariantCulture),
                    SeriesId = t.Title,
                    ProgramId = t.Id.ToString(CultureInfo.InvariantCulture),
                    ChannelId = t.ChannelId.ToString(CultureInfo.InvariantCulture),
                    StartDate = t.StartTime,
                    EndDate = t.EndTime,
                    IsPostPaddingRequired = (t.PostRecordInterval > 0),
                    IsPrePaddingRequired = (t.PreRecordInterval > 0),
                    PostPaddingSeconds = t.PostRecordInterval * 60,
                    PrePaddingSeconds = t.PreRecordInterval * 60,
                    Priority = t.Priority,
                    Overview = GeneralExtensions.TimerTypeDesc(t),
                };

                UpdateScheduling(seriesTimerInfo, t);

                return seriesTimerInfo;
            });

            Plugin.Logger.Info("Found series schedules: {0}", seriesSchedules.Count());
            return seriesSchedules;
        }

        private void UpdateScheduling(SeriesTimerInfo seriesTimerInfo, Schedule schedule)
        {
            var schedulingType = (WebScheduleType)schedule.ScheduleType;

            seriesTimerInfo.Days = new List<DayOfWeek>();
            seriesTimerInfo.RecordAnyChannel = false;
            seriesTimerInfo.RecordAnyTime = false;
            seriesTimerInfo.RecordNewOnly = false;
            seriesTimerInfo.SkipEpisodesInLibrary = false;

            if (seriesTimerInfo.Priority == 99)
            {
                seriesTimerInfo.SkipEpisodesInLibrary = true;
            }

            switch (schedulingType)
            {
                case WebScheduleType.EveryTimeOnThisChannel:
                    seriesTimerInfo.RecordAnyTime = true;
                    break;
                case WebScheduleType.EveryTimeOnEveryChannel:
                    seriesTimerInfo.RecordAnyChannel = true;
                    seriesTimerInfo.RecordAnyTime = true;
                    break;
                case WebScheduleType.WeeklyEveryTimeOnThisChannel:
                    seriesTimerInfo.Days.Add(schedule.StartTime.ToLocalTime().DayOfWeek);
                    seriesTimerInfo.RecordAnyTime = true;
                    seriesTimerInfo.RecordNewOnly = true;
                    break;
                case WebScheduleType.Daily:
                    seriesTimerInfo.Days.AddRange(new[]
                        {
                            DayOfWeek.Monday,
                            DayOfWeek.Tuesday,
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday,
                            DayOfWeek.Saturday,
                            DayOfWeek.Sunday,
                        });
                    break;
                case WebScheduleType.WorkingDays:
                    seriesTimerInfo.RecordNewOnly = true;
                    seriesTimerInfo.Days.AddRange(new[]
                        {
                            DayOfWeek.Monday,
                            DayOfWeek.Tuesday,
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday,
                        });
                    break;
                case WebScheduleType.Weekends:
                    seriesTimerInfo.RecordNewOnly = true;
                    seriesTimerInfo.Days.AddRange(new[]
                        {
                           DayOfWeek.Saturday,
                           DayOfWeek.Sunday,
                        });
                    break;
                case WebScheduleType.Weekly:
                    seriesTimerInfo.RecordNewOnly = true;
                    seriesTimerInfo.Days.Add(schedule.StartTime.ToLocalTime().DayOfWeek);
                    break;

                default:
                    throw new InvalidOperationException(String.Format("Should not be processing scheduling for ScheduleType={0}", schedulingType));
            }
        }

        #endregion

        #region Create Methods

        public void CreateSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var programData = GetProgram(cancellationToken, timer.ProgramId);
            if (programData == null)
            {
                throw ExceptionHelper.CreateArgumentException("timer.ProgramId", "The ProgramId: {0} for Schedule: {1} could not be found", timer.ProgramId, timer.Name);
            }

            else if (programData != null && programData.IsRecordingSeriesCanceled)
            {
                var uncancelSchedule = GetFromService<WebBoolResult>(cancellationToken, "UnCancelSchedule?programId={0}", timer.ProgramId);
                if (!uncancelSchedule.Result)
                {
                    Plugin.Logger.Info("Could not uncancel existing Schedule: {0}, StartTime: {1}, Channel: {2}, try creating new schedule instead", programData.Title, programData.StartTime, programData.ChannelId);
                }
            }

            else if (programData != null && !programData.IsRecordingSeriesCanceled)
            {
                var builder = new StringBuilder("AddScheduleDetailed?");

                builder.AppendFormat("channelId={0}&", programData.ChannelId);
                builder.AppendFormat("title={0}&", programData.Title);
                builder.AppendFormat("startTime={0}&", programData.StartTime.ToLocalTime().ToUrlDate());
                builder.AppendFormat("endTime={0}&", programData.EndTime.ToLocalTime().ToUrlDate());
                builder.AppendFormat("scheduleType={0}&", (Int32)WebScheduleType.Once);

                if (timer.IsPrePaddingRequired & timer.PrePaddingSeconds > 0)
                {
                    builder.AppendFormat("preRecordInterval={0}&", timer.PrePaddingSeconds / 60);
                }

                if (timer.IsPostPaddingRequired & timer.PostPaddingSeconds > 0)
                {
                    builder.AppendFormat("postRecordInterval={0}&", timer.PostPaddingSeconds / 60);
                }

                builder.Remove(builder.Length - 1, 1);

                var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
                if (response.Result)
                {
                    Plugin.Logger.Info("Created new Schedule: {0}, StartTime: {1}, EndTime: {2}, Channel: {3}", programData.Title, programData.StartTime, programData.EndTime, programData.ChannelId);
                }
                else
                {
                    throw new LiveTvConflictException();
                }
            }

            RefreshSchedules(cancellationToken);
        }

        public void ChangeSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var timerData = GetSchedule(cancellationToken, timer.Id);
            if (timerData == null)
            {
                var programData = GetProgram(cancellationToken, timer.ProgramId);
                if (programData == null)
                {
                    throw ExceptionHelper.CreateArgumentException("timer.ProgramId", "The ProgramId: {0} for Schedule: {1} could not be found", timer.ProgramId, timer.Name);
                }
                else if (programData != null && (!programData.IsScheduled || programData.IsRecordingSeriesCanceled))
                {
                    var uncancelSchedule = GetFromService<WebBoolResult>(cancellationToken, "UnCancelSchedule?programId={0}", timer.ProgramId);
                    if (!uncancelSchedule.Result)
                    {
                        Plugin.Logger.Info("Could not uncancel existing Schedule: {0}, StartTime: {1}, Channel: {2}, try creating new schedule instead", programData.Title, programData.StartTime, programData.ChannelId);
                    }
                }
            }

            else
            {
                var builder = new StringBuilder("EditSchedule?");

                builder.AppendFormat("scheduleId={0}&", timer.Id);
                builder.AppendFormat("channelId={0}&", timer.ChannelId);
                builder.AppendFormat("title={0}&", timerData.Title);
                builder.AppendFormat("startTime={0}&", timer.StartDate.ToLocalTime().ToUrlDate());
                builder.AppendFormat("endTime={0}&", timer.EndDate.ToLocalTime().ToUrlDate());
                builder.AppendFormat("scheduleType={0}&", (Int32)WebScheduleType.Once);

                if (timer.IsPrePaddingRequired & timer.PrePaddingSeconds > 0)
                {
                    builder.AppendFormat("preRecordInterval={0}&", timer.PrePaddingSeconds / 60);
                }

                if (timer.IsPostPaddingRequired & timer.PostPaddingSeconds > 0)
                {
                    builder.AppendFormat("postRecordInterval={0}&", timer.PostPaddingSeconds / 60);
                }

                builder.Remove(builder.Length - 1, 1);

                var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
                if (response.Result)
                {
                    Plugin.Logger.Info("Changed Schedule: {0}, StartTime: {1}, EndTime: {2}, Channel: {3}", timerData.Title, timer.StartDate, timer.EndDate, timer.ChannelId);
                }
                else
                {
                    throw new LiveTvConflictException();
                }
            }

            RefreshSchedules(cancellationToken);
        }

        public void CreateSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo schedule)
        {
            var programData = GetProgram(cancellationToken, schedule.ProgramId);
            if (programData == null)
            {
                throw ExceptionHelper.CreateArgumentException("schedule.ProgramId", "The ProgramId: {0} for Schedule: {1} could not be found", schedule.ProgramId, schedule.Name);
            }

            var builder = new StringBuilder("AddScheduleDetailed?");

            builder.AppendFormat("channelId={0}&", programData.ChannelId);
            builder.AppendFormat("title={0}&", programData.Title);
            builder.AppendFormat("startTime={0}&", programData.StartTime.ToLocalTime().ToUrlDate());
            builder.AppendFormat("endTime={0}&", programData.EndTime.ToLocalTime().ToUrlDate());
            //builder.AppendFormat("scheduleType={0}&", (Int32)schedule.ToScheduleType());
            builder.AppendFormat("scheduleType={0}&", Configuration.SeriesTimerType());

            if (schedule.IsPrePaddingRequired & schedule.PrePaddingSeconds > 0)
            {
                builder.AppendFormat("preRecordInterval={0}&", TimeSpan.FromSeconds(schedule.PrePaddingSeconds).RoundUpMinutes());
            }

            if (schedule.IsPostPaddingRequired & schedule.PostPaddingSeconds > 0)
            {
                builder.AppendFormat("postRecordInterval={0}&", TimeSpan.FromSeconds(schedule.PostPaddingSeconds).RoundUpMinutes());
            }

            if (schedule.SkipEpisodesInLibrary)
            {
                builder.AppendFormat("priority=99&");
            }

            builder.Remove(builder.Length - 1, 1);

            var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
            if (response.Result)
            {
                Plugin.Logger.Info("Created new SeriesSchedule: {0}, StartTime: {1}, EndTime: {2}, Channel: {3}", programData.Title, programData.StartTime, programData.EndTime, programData.ChannelId);
                RefreshSchedules(cancellationToken);
            }
            else
            {
                throw new LiveTvConflictException();
            }
        }

        public void ChangeSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo schedule)
        {
            var timerData = GetSchedule(cancellationToken, schedule.Id);
            if (timerData == null)
            {
                throw ExceptionHelper.CreateArgumentException("schedule.Id", "The ScheduleId: {0} for SeriesSchedule: {1} could not be found", schedule.Id, schedule.Name);
            }

            var builder = new StringBuilder("EditSchedule?");

            builder.AppendFormat("scheduleId={0}&", timerData.Id);
            builder.AppendFormat("channelId={0}&", timerData.ChannelId);
            builder.AppendFormat("title={0}&", timerData.Title);
            builder.AppendFormat("startTime={0}&", timerData.StartTime.ToLocalTime().ToUrlDate());
            builder.AppendFormat("endTime={0}&", timerData.EndTime.ToLocalTime().ToUrlDate());
            builder.AppendFormat("scheduleType={0}&", (Int32)schedule.ToScheduleType());

            if (schedule.IsPrePaddingRequired & schedule.PrePaddingSeconds > 0)
            {
                builder.AppendFormat("preRecordInterval={0}&", TimeSpan.FromSeconds(schedule.PrePaddingSeconds).RoundUpMinutes());
            }

            if (schedule.IsPostPaddingRequired & schedule.PostPaddingSeconds > 0)
            {
                builder.AppendFormat("postRecordInterval={0}&", TimeSpan.FromSeconds(schedule.PostPaddingSeconds).RoundUpMinutes());
            }

            if (schedule.SkipEpisodesInLibrary)
            {
                builder.AppendFormat("priority=99&");
            }
            else
            {
                builder.AppendFormat("priority=0&");
            }

            builder.Remove(builder.Length - 1, 1);

            var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
            if (response.Result)
            {
                Plugin.Logger.Info("Changed SeriesSchedule: {0}, StartTime: {1}, EndTime: {2}, Channel: {3}", schedule.Name, schedule.StartDate, schedule.EndDate, schedule.ChannelId);
                RefreshSchedules(cancellationToken);
            }
            else
            {
                throw new LiveTvConflictException();
            }
        }

        #endregion

        #region Delete Methods

        public void DeleteSchedule(CancellationToken cancellationToken, string scheduleId)
        {
            try
            {
                var schedule = GetSchedule(cancellationToken, scheduleId);
                if (schedule != null)
                {
                    if (((schedule.StartTime - TimeSpan.FromMinutes(schedule.PreRecordInterval)) < DateTimeOffset.UtcNow)
                        && (DateTimeOffset.UtcNow < (schedule.EndTime + TimeSpan.FromMinutes(schedule.PostRecordInterval)))
                        && (schedule.ScheduleType == 0))
                    {
                        string scheduledProgram = GetFromService<List<ScheduledRecording>>(cancellationToken, "GetScheduledRecordingsForDate?date={0}", schedule.StartTime.ToLocalTime().Date).Where(s => s.ScheduleId == scheduleId).Select(s => s.ProgramId).FirstOrDefault().ToString();
                        GetFromService<WebBoolResult>(cancellationToken, "CancelSchedule?programId={0}", scheduledProgram);
                    }
                    else
                    {
                        GetFromService<WebBoolResult>(cancellationToken, "DeleteSchedule?scheduleId={0}", scheduleId);
                    }
                }
                else
                {
                    GetFromService<WebBoolResult>(cancellationToken, "CancelSchedule?programId={0}", scheduleId);
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.OfType<HttpException>().All(e => e.StatusCode != HttpStatusCode.NotFound))
                {
                    throw;
                }
            }
            
            RefreshSchedules(cancellationToken);
        }

        public void DeleteRecording(string recordingId, CancellationToken cancellationToken)
        {
            try
            {
                var recording = GetRecording(cancellationToken, recordingId);
                if (recording.IsRecording)
                {
                    DeleteSchedule(cancellationToken, recording.ScheduleId.ToString(CultureInfo.InvariantCulture));
                }

                var response = GetFromService<WebBoolResult>(cancellationToken, "DeleteRecording?id={0}", recordingId);
                if (!response.Result)
                {
                    throw new LiveTvConflictException();
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.OfType<HttpException>().All(e => e.StatusCode != HttpStatusCode.NotFound))
                {
                    throw;
                }
            }
        }

        #endregion

        #region Streaming Methods

        public bool CancelCurrentTimeshifting(CancellationToken cancellationToken, string streamIdentifier)
        {
            return GetFromService<WebBoolResult>(cancellationToken, "CancelCurrentTimeshifting?userName={0}", streamIdentifier).Result;
        }

        public string GetLiveTvRtspUrl(CancellationToken cancellationToken, string streamIdentifier, string channelId)
        {
            return GetFromService<WebStringResult>(cancellationToken, "SwitchTVServerToChannelAndGetStreamingUrl?userName={0}&channelId={1}", streamIdentifier, channelId).Result;
        }

        public string GetRecordingRtspUrl(CancellationToken cancellationToken, string recordingId)
        {
            return GetFromService<WebStringResult>(cancellationToken, "GetRecordingRtspUrl?id={0}", recordingId).Result;
        }

        #endregion

        #region Other Methods

        public ScheduleDefaults GetScheduleDefaults(CancellationToken cancellationToken)
        {
            Int32 preRecordMinutes;
            Int32 postRecordMinutes;

            if (!Int32.TryParse(ReadSettingFromDatabase(cancellationToken, "preRecordInterval"), out preRecordMinutes))
            {
                Plugin.Logger.Warn("Unable to read the setting 'preRecordInterval' from MP");
            }

            if (!Int32.TryParse(ReadSettingFromDatabase(cancellationToken, "postRecordInterval"), out postRecordMinutes))
            {
                Plugin.Logger.Warn("Unable to read the setting 'postRecordInterval' from MP");
            }

            return new ScheduleDefaults()
            {
                PreRecordInterval = TimeSpan.FromMinutes(preRecordMinutes),
                PostRecordInterval = TimeSpan.FromMinutes(postRecordMinutes),
            };
        }

        public String ReadSettingFromDatabase(CancellationToken cancellationToken, String name)
        {
            return GetFromService<WebStringResult>(cancellationToken, "ReadSettingFromDatabase?tagName={0}", name).Result;
        }

        #endregion
    }
}