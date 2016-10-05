using System.Collections.Generic;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class FFProbeMediaInfo
    {
        public FFProbeMediaFormatInfo format { get; set; }
        public FFProbeMediaStreamInfo[] streams { get; set; }
    }

    public class FFProbeMediaFormatInfo
    {
        public string filename { get; set; }
        public int nb_streams { get; set; }
        public string format_name { get; set; }
        public string format_long_name { get; set; }
        public string start_time { get; set; }
        public string duration { get; set; }
        public string size { get; set; }
        public int? bit_rate { get; set; }
        public int probe_score { get; set; }
        public Dictionary<string, string> tags { get; set; }
    }

    public class FFProbeMediaStreamInfo
    {
        public int index { get; set; }
        public string profile { get; set; }
        public string codec_name { get; set; }
        public string codec_long_name { get; set; }
        public string codec_type { get; set; }
        public string sample_rate { get; set; }
        public int channels { get; set; }
        public string channel_layout { get; set; }
        public string avg_frame_rate { get; set; }
        public string duration { get; set; }
        public int bit_rate { get; set; }
        public int width { get; set; }
        public int refs { get; set; }
        public int height { get; set; }
        public string display_aspect_ratio { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public int bits_per_sample { get; set; }
        public int bits_per_raw_sample { get; set; }
        public string r_frame_rate { get; set; }
        public int has_b_frames { get; set; }
        public string sample_aspect_ratio { get; set; }
        public string pix_fmt { get; set; }
        public int level { get; set; }
        public string time_base { get; set; }
        public string start_time { get; set; }
        public string codec_time_base { get; set; }
        public string codec_tag { get; set; }
        public string codec_tag_string { get; set; }
        public string sample_fmt { get; set; }
        public string dmix_mode { get; set; }
        public string start_pts { get; set; }
        public string is_avc { get; set; }
        public string nal_length_size { get; set; }
        public string ltrt_cmixlev { get; set; }
        public string ltrt_surmixlev { get; set; }
        public string loro_cmixlev { get; set; }
        public string loro_surmixlev { get; set; }
        public Dictionary<string, string> disposition { get; set; }
    }
}
