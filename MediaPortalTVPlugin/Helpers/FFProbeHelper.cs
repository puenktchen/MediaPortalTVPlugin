using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using MediaBrowser.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class FFProbeHelper
    {
        /// <summary>
        /// Converts ffprobe stream info to Emby MediaStream class
        /// </summary>
        /// <param name="streamInfo">The stream info.</param>
        /// <param name="formatInfo">The format info.</param>
        /// <returns>MediaStream.</returns>
        public static MediaStream GetMediaStream(FFProbeMediaStreamInfo streamInfo, FFProbeMediaFormatInfo formatInfo)
        {
            var stream = new MediaStream
            {
                Codec = streamInfo.codec_name,
                Index = streamInfo.index,
                Profile = streamInfo.profile,
                Level = streamInfo.level,
                PixelFormat = streamInfo.pix_fmt,
            };

            if (streamInfo.tags != null)
            {
                stream.Language = GetDictionaryValue(streamInfo.tags, "language");
            }
            if (string.Equals(streamInfo.codec_type, "audio", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Audio;
                stream.Channels = streamInfo.channels;
                stream.ChannelLayout = ParseChannelLayout(streamInfo.channel_layout);
                if (!string.IsNullOrEmpty(streamInfo.sample_rate))
                {
                    int value;
                    if (int.TryParse(streamInfo.sample_rate, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    {
                        stream.SampleRate = value;
                    }
                }
                if (streamInfo.bits_per_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_sample;
                }
                else if (streamInfo.bits_per_raw_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_raw_sample;
                }
            }
            else if (string.Equals(streamInfo.codec_type, "subtitle", StringComparison.OrdinalIgnoreCase) && streamInfo.codec_name != null)
            {
                stream.Type = MediaStreamType.Subtitle;
            }
            else if (string.Equals(streamInfo.codec_type, "video", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Video;
                stream.Width = streamInfo.width;
                stream.Height = streamInfo.height;
                stream.BitRate = (String.Equals(Plugin.Instance.Configuration.StreamingProfileName, "Direct", StringComparison.OrdinalIgnoreCase) && streamInfo.height <= 576) ? 4000000 :
                                 (String.Equals(Plugin.Instance.Configuration.StreamingProfileName, "Direct", StringComparison.OrdinalIgnoreCase) && (streamInfo.height > 576 || streamInfo.height <= 720)) ? 7000000 :
                                 (String.Equals(Plugin.Instance.Configuration.StreamingProfileName, "Direct", StringComparison.OrdinalIgnoreCase) && streamInfo.height > 720) ? 9000000 :
                                 streamInfo.bit_rate;
                stream.AspectRatio = GetAspectRatio(streamInfo);
                stream.AverageFrameRate = GetFrameRate(streamInfo.avg_frame_rate);
                stream.RealFrameRate = GetFrameRate(streamInfo.r_frame_rate);
                if (!(streamInfo.width == 1280 || streamInfo.height == 720))
                {
                    stream.IsInterlaced = true;
                }
                if (streamInfo.bits_per_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_sample;
                }
                else if (streamInfo.bits_per_raw_sample > 0)
                {
                    stream.BitDepth = streamInfo.bits_per_raw_sample;
                }
                stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase);
                if (streamInfo.refs > 0)
                {
                    stream.RefFrames = streamInfo.refs;
                }
            }
            else
            {
                return null;
            }
            if (streamInfo.disposition != null)
            {
                var isDefault = GetDictionaryValue(streamInfo.disposition, "default");
                var isForced = GetDictionaryValue(streamInfo.disposition, "forced");
                stream.IsDefault = string.Equals(isDefault, "1", StringComparison.OrdinalIgnoreCase);
                stream.IsForced = string.Equals(isForced, "1", StringComparison.OrdinalIgnoreCase);
            }
            return stream;
        }
        private static string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }
            string val;
            tags.TryGetValue(key, out val);
            return val;
        }
        private static string ParseChannelLayout(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.Split('(').FirstOrDefault();
        }
        private static string GetAspectRatio(FFProbeMediaStreamInfo info)
        {
            var original = info.display_aspect_ratio;
            int height;
            int width;
            var parts = (original ?? string.Empty).Split(':');
            if (!(parts.Length == 2 && int.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out width) &&
                int.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out height) &&
                width > 0 &&
                height > 0))
            {
                width = info.width;
                height = info.height;
            }
            if (width > 0 && height > 0)
            {
                double ratio = width;
                ratio /= height;
                if (IsClose(ratio, 1.777777778, .03))
                {
                    return "16:9";
                }
                if (IsClose(ratio, 1.3333333333, .05))
                {
                    return "4:3";
                }
                if (IsClose(ratio, 1.41))
                {
                    return "1.41:1";
                }
                if (IsClose(ratio, 1.5))
                {
                    return "1.5:1";
                }
                if (IsClose(ratio, 1.6))
                {
                    return "1.6:1";
                }
                if (IsClose(ratio, 1.66666666667))
                {
                    return "5:3";
                }
                if (IsClose(ratio, 1.85, .02))
                {
                    return "1.85:1";
                }
                if (IsClose(ratio, 2.35, .025))
                {
                    return "2.35:1";
                }
                if (IsClose(ratio, 2.4, .025))
                {
                    return "2.40:1";
                }
            }
            return original;
        }
        private static bool IsClose(double d1, double d2, double variance = .005)
        {
            return Math.Abs(d1 - d2) <= variance;
        }
        private static float? GetFrameRate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split('/');
                float result;
                if (parts.Length == 2)
                {
                    result = float.Parse(parts[0], CultureInfo.InvariantCulture) / float.Parse(parts[1], CultureInfo.InvariantCulture);
                }
                else
                {
                    result = float.Parse(parts[0], CultureInfo.InvariantCulture);
                }
                return float.IsNaN(result) ? (float?)null : result;
            }
            return null;
        }
    }
}