using System;
using System.Text.RegularExpressions;

namespace MediaBrowser.Plugins.MediaPortal.Entities
{
    public class Program
    {
        #region General Informations

        public int Id { get; set; }
        public int ChannelId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public int ParentalRating { get; set; }
        public string Classification { get; set; }
        public int StarRating { get; set; }
        public int DurationInMinutes { get; set; }

        #endregion

        #region Series Informations

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

        private DateTime originalAirDate;
        public DateTimeOffset OriginalAirDate
        {
            get
            {
                return DateTime.SpecifyKind(originalAirDate, DateTimeKind.Utc);
            }
            set
            {
                originalAirDate = value.DateTime;
            }
        }

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

        #endregion
    }
}