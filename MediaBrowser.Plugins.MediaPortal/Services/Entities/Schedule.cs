using System;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class Schedule
    {
        #region General Informations

        public int Id { get; set; }
        public string Title { get; set; }
        public int ChannelId { get; set; }
        public string Directory { get; set; }

        #endregion

        #region Series Information

        public bool Series { get; set; }
        public int ParentScheduleId { get; set; }
        public bool DoesUseEpisodeManagement { get; set; }

        #endregion

        #region Timer Informations

        public int ScheduleType { get; set; }
        public int Priority { get; set; }
        public int PreRecordInterval { get; set; }
        public int PostRecordInterval { get; set; }

        public bool IsChanged { get; set; }
        public bool IsManual { get; set; }
        public long MaxAirings { get; set; }
        public int KeepMethod { get; set; }
        public DateTimeOffset KeepDate { get; set; }

        public int BitRateMode { get; set; }
        public int Quality { get; set; }
        public int QualityType { get; set; }
        public int RecommendedCard { get; set; }

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

        private DateTime canceled;
        public DateTimeOffset Canceled
        {
            get
            {
                return DateTime.SpecifyKind(canceled, DateTimeKind.Utc);
            }
            set
            {
                canceled = value.DateTime;
            }
        }

        #endregion
    }
}