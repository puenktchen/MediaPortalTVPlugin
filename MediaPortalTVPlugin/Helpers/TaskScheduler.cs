using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class RefreshRecordingsTask : IScheduledTask, IConfigurableScheduledTask
    {
        public string Category => "Live TV";
        public string Key => "MediaPortalRefreshRecordingsTask";
        public string Name => "Refresh recordings with changes from MediaPortal TVServer";
        public string Description => "Checks for recording changes from MediaPortal TVServer and initiates a recording refresh in Emby";

        public bool IsHidden => true;
        public bool IsEnabled => (Plugin.Instance.Configuration.EnableTimerCache) ? true : false;
        public bool IsLogged => false;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            TaskTriggerInfo startuptrigger = new TaskTriggerInfo();
            startuptrigger.Type = "StartupTrigger";

            TaskTriggerInfo trigger = new TaskTriggerInfo();
            trigger.IntervalTicks = 600000000;
            trigger.Type = "IntervalTrigger";

            List<TaskTriggerInfo> tasktrigger = new List<TaskTriggerInfo>();
            tasktrigger.Add(startuptrigger);
            tasktrigger.Add(trigger);

            return tasktrigger;
        } 

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            MediaPortal1TvService tvservice = new MediaPortal1TvService();
            tvservice.CheckRecordingStatus(cancellationToken);
            return Task.Delay(0, cancellationToken);
        }
    }

    public class RefreshTimersTask : IScheduledTask, IConfigurableScheduledTask
    {
        public string Category => "Live TV";
        public string Key => "MediaPortalRefreshTimersTask";
        public string Name => "Refresh timers with changes from MediaPortal TVServer";
        public string Description => "Refreshes timers with changes from MediaPortal TVServer";

        public bool IsHidden => true;
        public bool IsEnabled => (Plugin.Instance.Configuration.EnableTimerCache) ? true : false;
        public bool IsLogged => false;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            TaskTriggerInfo startuptrigger = new TaskTriggerInfo();
            startuptrigger.Type = "StartupTrigger";

            TaskTriggerInfo intervaltrigger = new TaskTriggerInfo();
            intervaltrigger.IntervalTicks = 9000000000;
            intervaltrigger.Type = "IntervalTrigger";

            List<TaskTriggerInfo> tasktrigger = new List<TaskTriggerInfo>();
            tasktrigger.Add(startuptrigger);
            tasktrigger.Add(intervaltrigger);

            return tasktrigger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            MediaPortal1TvService tvservice = new MediaPortal1TvService();
            tvservice.RefreshTimers(cancellationToken);
            tvservice.RefreshSeriesTimers(cancellationToken);
            return Task.Delay(0, cancellationToken);
        }
    }
}
