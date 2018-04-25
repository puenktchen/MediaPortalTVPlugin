using System;
using System.Collections.Generic;
using System.Linq;

using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public static class GeneralExtensions
    {
        public static String ToUrlDate(this DateTime value)
        {
            return value.ToString("s");
        }

        public static Boolean IsDaily(this List<DayOfWeek> value)
        {
            return value.Contains(DayOfWeek.Monday) &&
                   value.Contains(DayOfWeek.Tuesday) &&
                   value.Contains(DayOfWeek.Wednesday) &&
                   value.Contains(DayOfWeek.Thursday) &&
                   value.Contains(DayOfWeek.Friday) &&
                   value.Contains(DayOfWeek.Saturday) &&
                   value.Contains(DayOfWeek.Sunday);
        }

        public static Boolean IsWorkingDays(this List<DayOfWeek> value)
        {
            return value.Count == 5 &&
                   value.Contains(DayOfWeek.Monday) &&
                   value.Contains(DayOfWeek.Tuesday) &&
                   value.Contains(DayOfWeek.Wednesday) &&
                   value.Contains(DayOfWeek.Thursday) &&
                   value.Contains(DayOfWeek.Friday);
        }

        public static Boolean IsWeekends(this List<DayOfWeek> value)
        {
            return value.Count == 2 && 
                   value.Contains(DayOfWeek.Saturday) &&
                   value.Contains(DayOfWeek.Sunday);
        }

        public static WebScheduleType ToScheduleType(this SeriesTimerInfo info)
        {
            ///// Once = 0
            //if (!info.RecordAnyTime && !info.RecordAnyChannel && info.Days.Count == 0)
            //{
            //    return WebScheduleType.Once;
            //}

            ///// Daily = 1
            //else if (!info.RecordAnyTime && !info.RecordAnyChannel && info.Days.IsDaily())
            //{
            //    return WebScheduleType.Daily;
            //}

            ///// Weekly = 2
            //else if (!info.RecordAnyTime && !info.RecordAnyChannel && info.Days.Count == 1)
            //{
            //    return WebScheduleType.Weekly;
            //}

            ///// WeeklyEveryTimeOnThisChannel = 7
            //else if (info.RecordAnyTime && !info.RecordAnyChannel && info.Days.Count == 1)
            //{
            //    return WebScheduleType.WeeklyEveryTimeOnThisChannel;
            //}

            /////EveryTimeOnThisChannel = 3
            //else if (info.RecordAnyTime && !info.RecordAnyChannel && (info.Days.Count == 0 || info.Days.IsDaily()))
            //{
            //    return WebScheduleType.EveryTimeOnThisChannel;
            //}

            /////EveryTimeOnEveryChannel = 4
            //else if (info.RecordAnyChannel && (info.RecordAnyTime || !info.RecordAnyTime) && (info.Days.Count == 0 || info.Days.IsDaily()))
            //{
            //    return WebScheduleType.EveryTimeOnEveryChannel;
            //}

            ///// Weekends = 5
            //else if (info.Days.IsWeekends())
            //{
            //    return WebScheduleType.Weekends;
            //}

            ///// WorkingDays = 6
            //else if (info.Days.IsWorkingDays())
            //{
            //    return WebScheduleType.WorkingDays;
            //}

            ///// if we get here, then the user specified options that are not supported by MP
            //else
            //{
            //    return WebScheduleType.EveryTimeOnThisChannel;
            //}


            /// Daily = 1
            if (!info.RecordAnyTime && !info.RecordAnyChannel && !info.RecordNewOnly)
            {
                return WebScheduleType.Daily;
            }

            /// Weekly = 2
            else if (!info.RecordAnyTime && !info.RecordAnyChannel && info.RecordNewOnly)
            {
                return WebScheduleType.Weekly;
            }

            /// WeeklyEveryTimeOnThisChannel = 7
            else if (info.RecordAnyTime && !info.RecordAnyChannel && info.RecordNewOnly)
            {
                return WebScheduleType.WeeklyEveryTimeOnThisChannel;
            }

            ///EveryTimeOnThisChannel = 3
            else if (info.RecordAnyTime && !info.RecordAnyChannel && !info.RecordNewOnly)
            {
                return WebScheduleType.EveryTimeOnThisChannel;
            }

            ///EveryTimeOnEveryChannel = 4
            else if (info.RecordAnyTime && info.RecordAnyChannel)
            {
                return WebScheduleType.EveryTimeOnEveryChannel;
            }

            /// if we get here, then the user specified options that are not supported by MP
            else
            {
                return WebScheduleType.EveryTimeOnThisChannel;
            }
        }

        public static String TimerTypeDesc(this Schedule timer)
        {
            if (timer.ScheduleType == 1)
            {
                return "MediaPortal timer type: Daily";
            }
            else if (timer.ScheduleType == 2)
            {
                return String.Format("MediaPortal timer type: Weekly ({0})", timer.StartTime.ToLocalTime().DayOfWeek);
            }
            else if (timer.ScheduleType == 3)
            {
                return "MediaPortal timer type: Every Time On This Channel";
            }
            else if (timer.ScheduleType == 4)
            {
                return "MediaPortal timer type: Every Time On Every Channel";
            }
            else if (timer.ScheduleType == 5)
            {
                return "MediaPortal timer type: Weekends";
            }
            else if (timer.ScheduleType == 6)
            {
                return "MediaPortal timer type: Working Days";
            }
            else if (timer.ScheduleType == 7)
            {
                return String.Format("MediaPortal timer type: Weekly ({0}) Every Time On This Channel", timer.StartTime.ToLocalTime().DayOfWeek);
            }
            else
            {
                return "MediaPortal timer type: Unknown";
            }
        }

        public static IEnumerable<TResult> Process<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> func) where TResult : class
        {
            return source.Select(func).Where(result => result != null);
        }

        /// <summary>
        /// Rounds up.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="interval">The interval.</param>
        /// <returns></returns>
        public static TimeSpan RoundUp(this TimeSpan time, TimeSpan interval)
        {
            Int64 remainder;
            Math.DivRem(time.Ticks, interval.Ticks, out remainder);

            if (remainder == 0)
            {
                return time;
            }

            return TimeSpan.FromTicks(((time.Ticks + interval.Ticks + 1) / interval.Ticks) * interval.Ticks);
        }

        public static Int64 RoundUpMinutes(this TimeSpan time)
        {
            return (Int64)time.RoundUp(TimeSpan.FromMinutes(1)).TotalMinutes;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
