using System.Collections.Generic;

namespace MediaBrowser.Plugins.MediaPortal
{
    public class MediaPortalOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string TranscoderProfile { get; set; }
        public string TvChannelGroup { get; set; }
        public string RadioChannelGroup { get; set; }
        public bool ImportRadioChannels { get; set; }
        public Dictionary<string, List<string>> GenreMappings { get; set; }
    }
}
