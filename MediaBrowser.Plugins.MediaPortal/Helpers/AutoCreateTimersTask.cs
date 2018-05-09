using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;
using MediaBrowser.Plugins.MediaPortal.Services.Proxies;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class AutoCreateTimersTask : ProxyBase, IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;

        public AutoCreateTimersTask(IHttpClient httpClient, IJsonSerializer serialiser, ILibraryManager libraryManager)
            : base(httpClient, serialiser)
        {
            _libraryManager = libraryManager;
        }

        protected override string EndPointSuffix
        {
            get { return "TVAccessService/json"; }
        }

        public string Category => "Live TV";
        public string Key => "MediaPortalAutoCreateTimersTask";
        public string Name => "Autocreate MediaPortal timers";
        public string Description => "Gets guide data for 24 hours and autocreates new MediaPortal timers, based on missing Emby library episodes" +
                                     Environment.NewLine +
                                     "(The action should be executed some minutes after the guide refresh task has finished, but only once a day)";

        public bool IsHidden => Plugin.Instance.Configuration.AutoCreateTimers ? false : true;
        public bool IsEnabled => Plugin.Instance.Configuration.AutoCreateTimers ? true : false;
        public bool IsLogged => false;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            TaskTriggerInfo startuptrigger = new TaskTriggerInfo();
            startuptrigger.Type = "StartupTrigger";

            List<TaskTriggerInfo> tasktrigger = new List<TaskTriggerInfo>();
            tasktrigger.Add(startuptrigger);

            return tasktrigger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            SearchForTimers(cancellationToken);

            return Task.Delay(0, cancellationToken);
        }

        private void SearchForTimers(CancellationToken cancellationToken)
        {
            var missingEpisodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Episode).Name },
                IsMissing = true,
            });

            var programs = GetFromService<List<ProgramForGroup>>(
                cancellationToken,
                "GetProgramsDetailedForGroup?groupId={0}&startTime={1}&endTime={2}",
                Configuration.TvChannelGroup,
                DateTime.Now.ToLocalTime().ToUrlDate(),
                DateTime.Now.ToLocalTime().AddHours(25).ToUrlDate());

            foreach (var program in programs.SelectMany(x => x.Programs))
            {
                if (!String.IsNullOrEmpty(program.Title))
                {
                    foreach (var episode in missingEpisodes.Where(x =>
                    x.Parent.Parent.Name.Contains(Regex.Replace(program.Title, @"\s\W[a-zA-Z]?[0-9]{1,3}?\W$", String.Empty)) &&
                    x.IndexNumber.Equals(program.EpisodeNumber) &&
                    x.ParentIndexNumber.Equals(program.SeasonNumber)))
                    {
                        CreateTimer(program, cancellationToken);
                    }
                }
            }

            Plugin.TvProxy.RefreshSchedules(cancellationToken);
        }

        private void CreateTimer(Program program, CancellationToken cancellationToken)
        {
            var programData = GetFromService<Program>(cancellationToken, "GetProgramDetailedById?programId={0}", program.Id);

            if (programData.IsRecordingSeriesCanceled)
            {
                GetFromService<WebBoolResult>(cancellationToken, "UnCancelSchedule?programId={0}", program.Id);
            }

            if (!programData.IsRecordingSeriesCanceled && !programData.IsRecordingSeriesPending && !programData.IsScheduled)
            {
                Int32 preRecordMinutes;
                Int32 postRecordMinutes;

                Int32.TryParse(GetFromService<WebStringResult>(cancellationToken, "ReadSettingFromDatabase?tagName=preRecordInterval").Result, out preRecordMinutes);
                Int32.TryParse(GetFromService<WebStringResult>(cancellationToken, "ReadSettingFromDatabase?tagName=postRecordInterval").Result, out postRecordMinutes);

                var builder = new StringBuilder("AddScheduleDetailed?");

                builder.AppendFormat("channelId={0}&", programData.ChannelId);
                builder.AppendFormat("title={0}&", programData.Title);
                builder.AppendFormat("startTime={0}&", programData.StartTime.ToLocalTime().ToUrlDate());
                builder.AppendFormat("endTime={0}&", programData.EndTime.ToLocalTime().ToUrlDate());
                builder.AppendFormat("scheduleType={0}&", (Int32)WebScheduleType.Once);
                builder.AppendFormat("preRecordInterval={0}&", preRecordMinutes);
                builder.AppendFormat("postRecordInterval={0}&", postRecordMinutes);

                builder.Remove(builder.Length - 1, 1);

                GetFromService<WebBoolResult>(cancellationToken, builder.ToString());
            }
        }
    }
}