using System;
using System.Text.RegularExpressions;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class Program
    {
        public int ChannelId { get; set; }
        public string Description { get; set; }
        public int DurationInMinutes { get; set; }
        public DateTime EndTime { get; set; }
        public int Id { get; set; }
        public bool IsScheduled { get; set; }
        public DateTime StartTime { get; set; }
        public string Title { get; set; }
        public string Classification { get; set; }

        private string episodeName;
        public string EpisodeName
        {
            get
            {
                if (!String.IsNullOrEmpty(episodeName))
                {
                    return Regex.Replace(episodeName, @"(^[s]?[0-9]*[e|x|\.][0-9]*[^\w]+)|(\s[\(]?[s]?[0-9]*[e|x|\.][0-9]*[\)]?$)", String.Empty, RegexOptions.IgnoreCase);
                }
                return null;
            }
            set
            {
                episodeName = value;
            }
        }

        public string EpisodeNum { get; set; }
        public int? EpisodeNumber
        {
            get
            {
                if (EpisodeNum != null)
                {
                    int enumber;
                    if (Int32.TryParse((Regex.Match(EpisodeNum, @"\d+").Value), out enumber))
                    {
                        return enumber;
                    }
                }
                return null;
            }
        }

        public string EpisodePart { get; set; }
        public string Genre { get; set; }
        public bool HasConflict { get; set; }
        public bool IsChanged { get; set; }
        public bool IsPartialRecordingSeriesPending { get; set; }
        public bool IsRecording { get; set; }
        public bool IsRecordingManual { get; set; }
        public bool IsRecordingOnce { get; set; }
        public bool IsRecordingOncePending { get; set; }
        public bool IsRecordingSeries { get; set; }
        public bool IsRecordingSeriesPending { get; set; }
        public bool IsRecordingSeriesCanceled { get; set; }
        public bool Notify { get; set; }
        public DateTime OriginalAirDate { get; set; }
        public int ParentalRating { get; set; }

        public int? ProductionYear
        {
            get
            {
                if (!String.IsNullOrEmpty(Title))
                {
                    int year;
                    if (Int32.TryParse((Regex.Match(Title, @"(?<=\()\d{4}(?=\)$)").Value), out year))
                    {
                        return year;
                    }
                }
                return null;
            }
        }

        public string SeriesNum { get; set; }
        public int? SeasonNumber
        {
            get
            {
                if (SeriesNum != null)
                {
                    int snumber;
                    if (Int32.TryParse((Regex.Match(SeriesNum, @"\d+").Value), out snumber))
                    {
                        return snumber;
                    }
                }
                return null;
            }
        }

        public int StarRating { get; set; }
    }
}