using System.Collections.Generic;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class ProgramForGroup
    {
        public int ChannelId { get; set; }
        public List<Program> Programs { get; set;  }
    }
}