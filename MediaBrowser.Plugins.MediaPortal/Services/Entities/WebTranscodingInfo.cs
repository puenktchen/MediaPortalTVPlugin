namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class WebTranscodingInfo
    {
        public long TranscodedTime { get; set; }
        public long TranscodedFrames { get; set; }
        public long TranscodingPosition { get; set; }
        public long TranscodingFPS { get; set; }
        public long OutputBitrate { get; set; }
        public bool Supported { get; set; }
        public bool Finished { get; set; }
        public bool Failed { get; set; }

    }
}