using System;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class WebStreamingSession
    {
        public string Profile { get; set; }
        public string Identifier { get; set; }
        public WebMediaType SourceType { get; set; }
        public string SourceId { get; set; }
        public string DisplayName { get; set; }
        public string ClientDescription { get; set; }
        public string ClientIPAdress { get; set; }
        public DateTime StartTime { get; set; }
        public long StartPosition { get; set; }
        public long PlayerPosition { get; set; }
        public int PercentageProgress { get; set; }
        public WebTranscodingInfo TranscodingInfo { get; set; }

    }
}
