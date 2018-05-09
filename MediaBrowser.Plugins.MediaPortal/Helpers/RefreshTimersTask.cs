using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class RefreshTimersTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;

        public RefreshTimersTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public string Category => "Live TV";
        public string Key => "MediaPortalRefreshTimersTask";
        public string Name => "Refresh MediaPortal timers";
        public string Description => "Refresh MediaPortal timers and if enabled, check against Emby library items";

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => false;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            TaskTriggerInfo startuptrigger = new TaskTriggerInfo();
            startuptrigger.Type = "StartupTrigger";

            TaskTriggerInfo intervaltrigger = new TaskTriggerInfo();
            intervaltrigger.Type = "IntervalTrigger";
            intervaltrigger.IntervalTicks = 9000000000;

            List<TaskTriggerInfo> tasktrigger = new List<TaskTriggerInfo>();
            tasktrigger.Add(startuptrigger);
            tasktrigger.Add(intervaltrigger);

            return tasktrigger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            Plugin.TvProxy.RefreshSchedules(cancellationToken);

            SkipTimers(cancellationToken);

            return Task.Delay(0, cancellationToken);
        }

        private void SkipTimers(CancellationToken cancellationToken)
        {
            var timers = Plugin.TvProxy.GetSchedulesFromMemory(cancellationToken);
            var seriestimers = Plugin.TvProxy.GetSeriesSchedulesFromMemory(cancellationToken);

            foreach (var seriestimer in seriestimers.Where(x => x.Priority.Equals(99)))
            {
                foreach (var timer in timers.Where(x => x.Status.Equals(Model.LiveTv.RecordingStatus.New)))
                {
                    if (timer.SeriesTimerId == seriestimer.Id)
                    {
                        Plugin.Logger.Info("Series Schedule: {0} is marked for library watching", seriestimer.Name);
                        if (IsAlreadyInLibrary(timer))
                        {
                            Plugin.Logger.Info("Schedule: {0} exists already as Emby library item, trying delete now", timer.Name);
                            Plugin.TvProxy.DeleteSchedule(cancellationToken, timer.Id);
                        }
                    }
                }
            }
        }

        private bool IsAlreadyInLibrary(TimerInfo timer)
        {
            if (!String.IsNullOrEmpty(timer.Name) && !String.IsNullOrEmpty(timer.EpisodeTitle))
            {
                string seriesName = Regex.Replace(Regex.Split(timer.Name, @"\s\-\s").FirstOrDefault(), @"\s\W[a-zA-Z]?[0-9]{1,3}?\W$", String.Empty);
                string episodeName = Regex.Replace(timer.EpisodeTitle, @"(^[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[\(]?[s]?[0-9]*[e|x|\.][0-9]*[\)]?$)", String.Empty, RegexOptions.IgnoreCase);
                string movieName = Regex.Replace(timer.Name, @"\s\W[0-9]+\W$", String.Empty);

                if (Plugin.Instance.Configuration.SkipAlreadyInLibraryProfile == "Season and Episode Numbers" && timer.EpisodeNumber.HasValue && timer.SeasonNumber.HasValue)
                {
                    var seriesIds = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Series).Name },
                        Name = seriesName

                    }).ToArray();

                    if (seriesIds.Length == 0)
                    {
                        return false;
                    }

                    var episode = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Episode).Name },
                        ParentIndexNumber = timer.SeasonNumber.Value,
                        IndexNumber = timer.EpisodeNumber.Value,
                        AncestorIds = seriesIds,
                        IsVirtualItem = false,
                        Limit = 1
                    });

                    if (episode.Count > 0)
                    {
                        return true;
                    }
                }

                if (Plugin.Instance.Configuration.SkipAlreadyInLibraryProfile == "Episode Name" && !string.IsNullOrWhiteSpace(timer.EpisodeTitle))
                {
                    var seriesIds = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Series).Name },
                        NameContains = seriesName

                    }).ToArray();

                    if (seriesIds.Length == 0)
                    {
                        return false;
                    }

                    var episodename = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Episode).Name },
                        NameContains = episodeName,
                        AncestorIds = seriesIds,
                        IsVirtualItem = false,
                        Limit = 1
                    });

                    if (episodename.Count > 0)
                    {
                        return true;
                    }
                }

                if (timer.IsMovie)
                {
                    var movie = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Movie).Name },
                        NameContains = movieName

                    }).Select(i => i.ToString("N")).ToArray();

                    if (movie.Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}
