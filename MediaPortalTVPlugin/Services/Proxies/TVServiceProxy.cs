using System;
using System.Collections.Generic;
using System.Globalization;
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

        public TvServiceProxy(IHttpClient httpClient, IJsonSerializer serialiser, StreamingServiceProxy wssProxy)
            : base(httpClient, serialiser)
        {
            _wssProxy = wssProxy;
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

        public List<ChannelGroup> GetChannelGroups(CancellationToken cancellationToken)
        {
            return GetFromService<List<ChannelGroup>>(cancellationToken, "GetGroups").OrderBy(g => g.SortOrder).ToList();
        }
        
        public IEnumerable<ChannelInfo> GetChannels(CancellationToken cancellationToken)
        {
            var builder = new StringBuilder("GetChannelsDetailed");
            if (Configuration.DefaultChannelGroup > 0)
            {
                // This is the only way to get out the channels in the same order that MP displays them.
                builder.AppendFormat("?groupId={0}", Configuration.DefaultChannelGroup);
            }

            var channels = GetFromService<List<Channel>>(cancellationToken, builder.ToString());
            IEnumerable<Channel> query = channels;

            switch (Configuration.DefaultChannelSortOrder)
            {
                case ChannelSorting.ChannelName:
                    query = query.OrderBy(q => q.Title);
                    break;
                case ChannelSorting.ChannelId:
                    query = query.OrderBy(q => q.Id);
                    break;
            }

            Plugin.Logger.Info("Found channels: {0}", channels.Where(c => c.VisibleInGuide));
            return query.Where(c => c.VisibleInGuide).Select((c, index) => new ChannelInfo()
            {
                Name = c.Title,
                Id = c.Id.ToString(CultureInfo.InvariantCulture),
                ChannelType = c.IsTv ? ChannelType.TV : ChannelType.Radio,
                Number = (Configuration.ChannelByIndex) ? (index + 1).ToString("D1", CultureInfo.InvariantCulture) : " ",
                ImageUrl = _wssProxy.GetChannelLogoUrl(c.Id)
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

        public IEnumerable<ProgramInfo> GetPrograms(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var response = GetFromService<List<Program>>(
                cancellationToken,
                "GetProgramsDetailedForChannel?channelId={0}&startTime={1}&endTime={2}",
                channelId,
                startDateUtc.ToLocalTime().ToUrlDate(),
                endDateUtc.ToLocalTime().ToUrlDate());

            // Create this once per channel - if created at the class level, then changes to configuration would never be caught
            var genreMapper = new GenreMapper(Plugin.Instance.Configuration);

            Plugin.Logger.Info("Found programs: {0}  for channel id: {1}", response.Count(), channelId);
            return response.Select(p =>
            {
                var program = new ProgramInfo()
                {
                    Name = p.Title,
                    EpisodeTitle = p.EpisodeName,
                    Id = p.Id.ToString(CultureInfo.InvariantCulture),
                    SeriesId = p.Title,
                    ChannelId = channelId,
                    StartDate = p.StartTime,
                    EndDate = p.EndTime,
                    Overview = p.Description,
                    Genres = new List<String>(),
                    HasImage = false,
                };

                if (!String.IsNullOrEmpty(p.EpisodeNum))
                {
                    int enumber;
                    Int32.TryParse((Regex.Match(p.EpisodeNum, @"\d+").Value), out enumber);
                    program.EpisodeNumber = enumber;
                }

                if (!String.IsNullOrEmpty(p.SeriesNum))
                {
                    int snumber;
                    Int32.TryParse((Regex.Match(p.SeriesNum, @"\d+").Value), out snumber);
                    program.SeasonNumber = snumber;
                }

                //program.IsSeries = true; //is set by genreMapper
                if (!String.IsNullOrEmpty(p.Genre))
                {
                    program.Genres.Add(p.Genre);
                    genreMapper.PopulateProgramGenres(program);
                }

                return program;
            });

        }

        public Recording GetRecording(CancellationToken cancellationToken, String recordingId)
        {
            return GetFromService<Recording>(cancellationToken, "GetRecordingById?id={0}", recordingId);
        }

        public IEnumerable<RecordingInfo> GetRecordings(CancellationToken cancellationToken)
        {
            var configuration = Plugin.Instance.Configuration;
            var localpath = String.Format("{0}", configuration.LocalFilePath);
            var remotepath = String.Format("{0}", configuration.RemoteFilePath);

            var schedules = GetFromService<List<Schedule>>(cancellationToken, "GetSchedules");
            var recordings = GetFromService<List<Recording>>(cancellationToken, "GetRecordings").Select(r =>
            {
                var recording = new RecordingInfo()
                {
                    Name = r.Title,
                    EpisodeTitle = r.EpisodeName,
                    Id = r.Id,
                    TimerId = r.ScheduleId.ToString(CultureInfo.InvariantCulture),
                    ChannelId = r.ChannelId.ToString(CultureInfo.InvariantCulture),
                    StartDate = r.StartTime,
                    EndDate = r.EndTime,
                    Overview = r.Description,
                    Genres = new List<String>(),
                    Status = (r.IsRecording) ? RecordingStatus.InProgress : RecordingStatus.Completed,
                    HasImage = false,
                };

                if (!String.IsNullOrEmpty(r.EpisodeNum))
                {
                    int EpisodeNumber;
                    int SeasonNumber;

                    recording.IsSeries = true;
                    recording.ShowId = r.Title;
                    
                    if (String.IsNullOrEmpty(r.SeriesNum))
                    {
                        Int32.TryParse((Regex.Match(r.EpisodeNum, @"\d+").Value), out EpisodeNumber);
                        recording.EpisodeTitle = String.Format("E{0} - {1}", EpisodeNumber, r.EpisodeName);
                    }

                    if (!String.IsNullOrEmpty(r.SeriesNum))
                    {
                        Int32.TryParse((Regex.Match(r.SeriesNum, @"\d+").Value), out SeasonNumber);
                        Int32.TryParse((Regex.Match(r.EpisodeNum, @"\d+").Value), out EpisodeNumber);
                        recording.EpisodeTitle = String.Format("S{0}, E{1} - {2}", SeasonNumber, EpisodeNumber, r.EpisodeName);
                    }  
                }

                if (!r.IsRecording)
                {
                    recording.HasImage = true;
                    recording.ImageUrl = _wssProxy.GetRecordingImageUrl(r.Id);
                }

                //if (configuration.EnableDirectAccess && !configuration.RequiresPathSubstitution && !r.IsRecording)
                if (configuration.EnableDirectAccess && !configuration.RequiresPathSubstitution)
                {
                    recording.Path = r.FileName;
                }

                //if (configuration.EnableDirectAccess && configuration.RequiresPathSubstitution && !r.IsRecording)
                if (configuration.EnableDirectAccess && configuration.RequiresPathSubstitution)
                {
                    recording.Path = r.FileName.Replace(localpath, remotepath);
                }

                if (!String.IsNullOrEmpty(r.Genre))
                {
                    recording.Genres.Add(r.Genre);
                }

                var series = schedules.Where(s => (s.ScheduleType > 0 && s.Title == r.Title)).FirstOrDefault();
                if (series != null)
                {
                    recording.SeriesTimerId = series.Id.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Plugin.Logger.Info("The recording \"{0} - {1}\" does not match any series schedule", r.Title, r.EpisodeName);
                }

                return recording;

            }).ToList();

            Plugin.Logger.Info("Found recordings: {0} ", recordings.Count());
            return recordings;
        }

        private Schedule GetSchedule(CancellationToken cancellationToken, String Id)
        {
            return GetFromService<Schedule>(cancellationToken, "GetScheduleById?scheduleId={0}", Id);
        }

        public IEnumerable<TimerInfo> GetSchedules(CancellationToken cancellationToken)
        {
            List<TimerInfo> schedules = new List<TimerInfo>();

            var schedulesResponse = GetFromService<List<Schedule>>(cancellationToken, "GetSchedules");
            Plugin.Logger.Info("Found one time schedules: {0}", schedulesResponse.Where(c => c.ScheduleType == 0).Count());
            Plugin.Logger.Info("Found series schedules: {0}", schedulesResponse.Where(c => c.ScheduleType > 0).Count());

            foreach (var schedule in schedulesResponse.Where(s => s.ScheduleType == 0))
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
                timerInfo.Status = (((schedule.StartTime - TimeSpan.FromMinutes(schedule.PreRecordInterval) < DateTime.UtcNow)))
                                   && (DateTime.UtcNow < (schedule.EndTime + TimeSpan.FromMinutes(schedule.PostRecordInterval)))
                                   ? RecordingStatus.InProgress : RecordingStatus.New;

                var programResponse = GetFromService<List<Program>>(cancellationToken, "SearchProgramsDetailed?searchTerm={0}", schedule.Title);
                var program = programResponse.Where(p => (p.StartTime == schedule.StartTime && p.ChannelId == schedule.ChannelId)).FirstOrDefault();
                if (program != null)
                {
                    timerInfo.Name = (!String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName :
                                     (program.HasConflict && !String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName + " [Conflict]" :
                                     (program.HasConflict && String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " [Conflict]" :
                                     program.Title;
                    timerInfo.ProgramId = program.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.EpisodeTitle = program.EpisodeName;
                    timerInfo.Overview = program.Description;
                    timerInfo.Status = (program.HasConflict) ? RecordingStatus.ConflictedNotOk : timerInfo.Status;

                    if (!String.IsNullOrEmpty(program.EpisodeNum))
                    {
                        int enumber;
                        Int32.TryParse((Regex.Match(program.EpisodeNum, @"\d+").Value), out enumber);
                        timerInfo.EpisodeNumber = enumber;
                    }

                    if (!String.IsNullOrEmpty(program.SeriesNum))
                    {
                        int snumber;
                        Int32.TryParse((Regex.Match(program.SeriesNum, @"\d+").Value), out snumber);
                        timerInfo.SeasonNumber = snumber;
                    }

                    Plugin.Logger.Info("One time schedule: \"{0}\"; Channel: {1}; Start Time: {2}; End Time: {3}; Status: {4}", schedule.Title, schedule.ChannelId, schedule.StartTime, schedule.EndTime, timerInfo.Status.ToString());
                }
                else
                {
                    Plugin.Logger.Info("The one time schedule: \"{0}\" does not match any program with start time: {1} on channel: {2}", schedule.Title, schedule.StartTime, schedule.ChannelId);
                }
                schedules.Add(timerInfo);
            }

            foreach (var schedule in schedulesResponse.Where(s => s.ScheduleType > 0).DistinctBy(s => s.Title))
            {
                var programResponse = GetFromService<List<Program>>(cancellationToken, "SearchProgramsDetailed?searchTerm={0}", schedule.Title);
                Plugin.Logger.Info("Found pending timers: {0}  for series schedule: \"{1}\"", programResponse.Where(c => (c.IsRecordingSeriesPending || c.IsPartialRecordingSeriesPending || c.IsRecordingSeriesCanceled)).Count(), schedule.Title);

                foreach (var program in programResponse.Where(p => (p.IsRecordingSeriesPending || p.IsPartialRecordingSeriesPending || p.IsRecordingSeriesCanceled)))
                {
                    var timerInfo = new TimerInfo();
                    timerInfo.Name = (program.IsRecordingSeriesCanceled && !String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName + " [Cancelled]" :
                                     (program.HasConflict && !String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName + " [Conflict]" :
                                     (!String.IsNullOrEmpty(program.EpisodeNum)) ? program.Title + " - " + program.EpisodeName :
                                     program.Title;
                    timerInfo.Id = program.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.SeriesTimerId = schedule.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.ProgramId = program.Id.ToString(CultureInfo.InvariantCulture);
                    timerInfo.ChannelId = program.ChannelId.ToString(CultureInfo.InvariantCulture);
                    timerInfo.EpisodeTitle = program.EpisodeName;
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

                    if (!String.IsNullOrEmpty(program.EpisodeNum))
                    {
                        int enumber;
                        Int32.TryParse((Regex.Match(program.EpisodeNum, @"\d+").Value), out enumber);
                        timerInfo.EpisodeNumber = enumber;
                    }

                    if (!String.IsNullOrEmpty(program.SeriesNum))
                    {
                        int snumber;
                        Int32.TryParse((Regex.Match(program.SeriesNum, @"\d+").Value), out snumber);
                        timerInfo.SeasonNumber = snumber;
                    }

                    Plugin.Logger.Info("Seriespart schedule: \"{0}\"; Channel: {1}; Start Time: {2}; End Time: {3}; Status: {4}", program.Title, program.ChannelId, program.StartTime, program.EndTime, timerInfo.Status.ToString());

                    schedules.Add(timerInfo);
                };
            }

            return schedules;
        }

        public IEnumerable<SeriesTimerInfo> GetSeriesSchedules(CancellationToken cancellationToken)
        {
            var schedules = GetFromService<List<Schedule>>(cancellationToken, "GetSchedules").Where(r => r.ScheduleType > 0).Select(r =>
            {
                var seriesTimerInfo = new SeriesTimerInfo()
                {
                    Name = r.Title,
                    Id = r.Id.ToString(CultureInfo.InvariantCulture),
                    SeriesId = r.Title,
                    //SeriesId = r.Id.ToString(CultureInfo.InvariantCulture),
                    ProgramId = r.Id.ToString(CultureInfo.InvariantCulture),
                    ChannelId = r.ChannelId.ToString(CultureInfo.InvariantCulture),
                    StartDate = r.StartTime,
                    EndDate = r.EndTime,
                    IsPostPaddingRequired = (r.PostRecordInterval > 0),
                    IsPrePaddingRequired = (r.PreRecordInterval > 0),
                    PostPaddingSeconds = r.PostRecordInterval * 60,
                    PrePaddingSeconds = r.PreRecordInterval * 60,
                };

                UpdateScheduling(seriesTimerInfo, r);
                
                return seriesTimerInfo;

            });

            Plugin.Logger.Info("Found series schedules: {0}", schedules.Count());
            return schedules;
        }

        private void UpdateScheduling(SeriesTimerInfo seriesTimerInfo, Schedule schedule)
        {
            var schedulingType = (WebScheduleType)schedule.ScheduleType;

            // Initialise
            seriesTimerInfo.Days = new List<DayOfWeek>();
            seriesTimerInfo.RecordAnyChannel = false;
            seriesTimerInfo.RecordAnyTime = false;
            seriesTimerInfo.RecordNewOnly = false;

            switch (schedulingType)
            {
                case WebScheduleType.EveryTimeOnThisChannel:
                    seriesTimerInfo.RecordAnyTime = true;
                    break;
                case WebScheduleType.EveryTimeOnEveryChannel:
                    seriesTimerInfo.RecordAnyTime = true;
                    seriesTimerInfo.RecordAnyChannel = true;
                    break;
                case WebScheduleType.WeeklyEveryTimeOnThisChannel:
                    seriesTimerInfo.Days.Add(schedule.StartTime.ToLocalTime().DayOfWeek);
                    seriesTimerInfo.RecordAnyTime = true;
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
                    seriesTimerInfo.Days.AddRange(new[]
                        {
                           DayOfWeek.Saturday,
                           DayOfWeek.Sunday,
                        });
                    break;
                case WebScheduleType.Weekly:
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
                throw ExceptionHelper.CreateArgumentException("timer.ProgramId", "The program id {0} for \"{1}\" could not be found", timer.ProgramId, timer.Name);
            }

            else if (programData != null && programData.IsRecordingSeriesCanceled)
            {
                var uncancelSchedule = GetFromService<WebBoolResult>(cancellationToken, "UnCancelSchedule?programId={0}", timer.ProgramId);
                if (!uncancelSchedule.Result)
                {
                    Plugin.Logger.Info("Could not uncancel schedule for \"{0}\" with start time {1} on channel {2}, try creating new schedule instead", programData.Title, programData.StartTime, programData.ChannelId);
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

                Plugin.Logger.Info("Creating schedule with StartTime: {0}, EndTime: {1}, ProgramData from MP: {2}", timer.StartDate, timer.EndDate, builder.ToString());

                var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
                if (!response.Result)
                {
                    throw new LiveTvConflictException();
                }
            }

        }

        public void ChangeSchedule(CancellationToken cancellationToken, TimerInfo timer)
        {
            var timerData = GetSchedule(cancellationToken, timer.Id);
            if (timerData == null)
            {
                var programData = GetProgram(cancellationToken, timer.ProgramId);
                if (programData == null)
                {
                    throw ExceptionHelper.CreateArgumentException("timer.ProgramId", "The program id {0} for \"{1}\" could not be found", timer.ProgramId, timer.Name);
                }
                else if (programData != null && (!programData.IsScheduled || programData.IsRecordingSeriesCanceled))
                {
                    var uncancelSchedule = GetFromService<WebBoolResult>(cancellationToken, "UnCancelSchedule?programId={0}", timer.ProgramId);
                    if (!uncancelSchedule.Result)
                    {
                        Plugin.Logger.Info("Could not uncancel schedule for \"{0}\" with start time {1} on channel {2}, try changing schedule instead", programData.Title, programData.StartTime, programData.ChannelId);
                    }
                }
            }

            else
            {
                var builder = new StringBuilder("EditSchedule?");
                builder.AppendFormat("scheduleId={0}&", timer.Id);
                builder.AppendFormat("channelId={0}&", timer.ChannelId);
                builder.AppendFormat("title={0}&", timer.Name);
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

                Plugin.Logger.Info("Changed schedule with StartTime: {0}, EndTime: {1}, timerData from MP: {2}",
                    timer.StartDate, timer.EndDate, builder.ToString());

                var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
                if (!response.Result)
                {
                    throw new LiveTvConflictException();
                }
            }

        }

        public void CreateSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo schedule)
        {
            var programData = GetProgram(cancellationToken, schedule.ProgramId);
            if (programData == null)
            {
                throw ExceptionHelper.CreateArgumentException("schedule.ProgramId", "The program id {0} could not be found", schedule.ProgramId);
            }

            var builder = new StringBuilder("AddScheduleDetailed?");
            builder.AppendFormat("channelId={0}&", programData.ChannelId);
            builder.AppendFormat("title={0}&", programData.Title);
            builder.AppendFormat("startTime={0}&", programData.StartTime.ToLocalTime().ToUrlDate());
            builder.AppendFormat("endTime={0}&", programData.EndTime.ToLocalTime().ToUrlDate());
            builder.AppendFormat("scheduleType={0}&", (Int32)schedule.ToScheduleType());

            if (schedule.IsPrePaddingRequired & schedule.PrePaddingSeconds > 0)
            {
                builder.AppendFormat("preRecordInterval={0}&", TimeSpan.FromSeconds(schedule.PrePaddingSeconds).RoundUpMinutes());
            }

            if (schedule.IsPostPaddingRequired & schedule.PostPaddingSeconds > 0)
            {
                builder.AppendFormat("postRecordInterval={0}&", TimeSpan.FromSeconds(schedule.PostPaddingSeconds).RoundUpMinutes());
            }

            builder.Remove(builder.Length - 1, 1);

            Plugin.Logger.Info("Creating series schedule with StartTime: {0}, EndTime: {1}, ProgramData from MP: {2}",
                schedule.StartDate, schedule.EndDate, builder.ToString());

            var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
            if (!response.Result)
            {
                throw new LiveTvConflictException();
            }
        }

        public void ChangeSeriesSchedule(CancellationToken cancellationToken, SeriesTimerInfo schedule)
        {
            var timerData = GetSchedule(cancellationToken, schedule.Id);
            if (timerData == null)
            {
                throw ExceptionHelper.CreateArgumentException("schedule.Id", "The schedule id {0} could not be found", schedule.Id);
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

            builder.Remove(builder.Length - 1, 1);

            Plugin.Logger.Info("Changed series schedule with StartTime: {0}, EndTime: {1}, ProgramData from MP: {2}",
                schedule.StartDate, schedule.EndDate, builder.ToString());

            var response = GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
            if (!response.Result)
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
                    if (((schedule.StartTime - TimeSpan.FromMinutes(schedule.PreRecordInterval)) < DateTime.UtcNow)
                        && (DateTime.UtcNow < (schedule.EndTime + TimeSpan.FromMinutes(schedule.PostRecordInterval)))
                        && (schedule.ScheduleType == 0))
                    {
                        string scheduledProgram = GetFromService<List<ScheduledRecording>>(cancellationToken, "GetScheduledRecordingsForToday").Where(s => s.ScheduleId == scheduleId).Select(s => s.ProgramId).FirstOrDefault().ToString();
                        var cancelledProgram = GetFromService<WebBoolResult>(cancellationToken, "CancelSchedule?programId={0}", scheduledProgram);
                    }
                    else
                    {
                        var deleteResponse = GetFromService<WebBoolResult>(cancellationToken, "DeleteSchedule?scheduleId={0}", scheduleId);
                    }
                }
                else
                {
                    var cancelResponse = GetFromService<WebBoolResult>(cancellationToken, "CancelSchedule?programId={0}", scheduleId);
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

        public void DeleteRecording(string recordingId, CancellationToken cancellationToken)
        {
            try
            {
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
            return GetFromService<WebBoolResult>(cancellationToken, "CancelCurrentTimeshifting?userName=mpextended-{0}", streamIdentifier).Result;
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