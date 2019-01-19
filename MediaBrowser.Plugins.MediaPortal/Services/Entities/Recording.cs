using System;
using System.Text.RegularExpressions;

namespace MediaBrowser.Plugins.MediaPortal.Services.Entities
{
    public class Recording
    {
        #region General Informations

        public string Id { get; set; }
        public int ScheduleId { get; set; }
        public string FileName { get; set; }

        private string title;
        public string Title
        {
            get
            {
                if (!String.IsNullOrEmpty(title))
                {
                    return Regex.Replace(title, @"\s\W[a-zA-Z]?[0-9]{1,3}?\W$", String.Empty, RegexOptions.IgnoreCase);
                }
                return null;
            }
            set
            {
                title = value;
            }
        }

        public string MovieName
        {
            get
            {
                if (!String.IsNullOrEmpty(Title))
                {
                    return Regex.Replace(Title, @"(?<=\S)\s\W\d{4}\W(?=$)", String.Empty);
                }
                return null;
            }
        }

        public int? Year
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

        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public int StopTime { get; set; }
        public int TimesWatched { get; set; }

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

        #region Timer Informations

        public bool IsChanged { get; set; }
        public bool IsManual { get; set; }
        public bool IsRecording { get; set; }
        public int KeepUntil { get; set; }
        public bool ShouldBeDeleted { get; set; }

        private DateTime keepUntilDate;
        public DateTimeOffset KeepUntilDate
        {
            get
            {
                return DateTime.SpecifyKind(keepUntilDate, DateTimeKind.Utc);
            }
            set
            {
                keepUntilDate = value.DateTime;
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

        #endregion
    }
}