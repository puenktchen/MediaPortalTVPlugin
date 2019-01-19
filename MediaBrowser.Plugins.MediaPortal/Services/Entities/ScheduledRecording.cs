using System;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class ScheduledRecording
    {
        #region General Informations

        public string ScheduleId { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int ProgramId { get; set; }
        public string Title { get; set; }

        #endregion

        #region DateTime Informations

        private DateTime startTime;
        public DateTimeOffset StartTime
        {
            get
            {
                return DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
            }
            set
            {
                startTime = value.DateTime;
            }
        }

        private DateTime endTime;
        public DateTimeOffset EndTime
        {
            get
            {
                return DateTime.SpecifyKind(endTime, DateTimeKind.Utc);
            }
            set
            {
                endTime = value.DateTime;
            }
        }

        #endregion
    }
}