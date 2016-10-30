using System;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class ScheduledRecording
    {
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public DateTime EndTime { get; set; }
        public int ProgramId { get; set; }
        public string ScheduleId { get; set; }
        public DateTime StartTime { get; set; }
        public string Title { get; set; }
    }
}