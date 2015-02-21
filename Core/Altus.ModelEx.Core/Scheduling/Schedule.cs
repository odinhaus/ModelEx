using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core;
using System.Threading;
using Altus.Core.Threading;

namespace Altus.Core.Scheduling
{
    public enum IntervalType
    {
        Periodic,
        Calendrical,
        Composite
    }

    public abstract class Schedule
    {
        private List<Schedule> _items = new List<Schedule>();
        TwoPhaseWait _tpw;

        public Schedule(DateRange dateRange, IntervalType type)
        {
            DateRange = dateRange;
            IntervalType = type;
            _tpw = new TwoPhaseWait(TimeSpan.FromMilliseconds(this.Interval), false);
        }

        public DateRange DateRange { get; set; }
        public IntervalType IntervalType { get; private set; }
        public long Interval { get; protected set; }
        public TimeSpan EventDuration { get; protected set; }

        public virtual bool IsScheduled(long ticks)
        {
            return DateRange.Contains(ticks);
        }

        public void Add(Schedule schedule)
        {
            _items.Add(schedule);
        }

        /// <summary>
        /// Gets a schedule by ordinal position
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Schedule this[int index]
        {
            get { return _items[index]; }
        }

        /// <summary>
        /// Returns the most granular schedule for the provided date-time
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public Schedule this[DateTime dateTime]
        {
            get
            {
                return this[dateTime.Ticks];
            }
        }

        /// <summary>
        /// Return the most granual schedule for the provided Time.ticks
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public Schedule this[long ticks]
        {
            get
            {
                Schedule sched = this;
                foreach (Schedule s in _items)
                {
                    if (s.IsScheduled(ticks))
                    {
                        if (sched == this)
                        {
                            sched = s;
                        }
                        else if (s.DateRange.Duration == sched.DateRange.Duration
                            && s.IntervalType == Scheduling.IntervalType.Calendrical)
                        {
                            CalendricalSchedule cSched = (CalendricalSchedule)sched;
                            CalendricalSchedule cS = (CalendricalSchedule)s;

                            if (cSched.CalendricalScheduleType != CalendricalScheduleType.Fixed
                                && cS.CalendricalScheduleType == CalendricalScheduleType.Fixed)
                            {
                                // choose fixed events over recurring events
                                sched = s;
                            }
                            else if (cSched.CalendricalScheduleType != CalendricalScheduleType.DailyRecurring
                                && cS.CalendricalScheduleType == CalendricalScheduleType.DailyRecurring)
                            {
                                sched = s;
                            }
                            else if (cSched.CalendricalScheduleType != CalendricalScheduleType.WeeklyRecurring
                                && cS.CalendricalScheduleType == CalendricalScheduleType.WeeklyRecurring)
                            {
                                sched = s;
                            }
                            else if (cSched.CalendricalScheduleType != CalendricalScheduleType.MonthlyRecurring
                                && cS.CalendricalScheduleType == CalendricalScheduleType.MonthlyRecurring)
                            {
                                sched = s;
                            }
                        }
                        else if (s.DateRange.Duration < sched.DateRange.Duration) // choose shortest scheduled items
                        {
                            sched = s;
                        }
                    }
                }

                if (sched != this)
                    sched = sched[ticks]; // recurse to find the narrowest child

                return sched;
            }
        }

        /// <summary>
        /// Blocks the calling thread until the next scheduled event takes place
        /// </summary>
        /// <param name="lastExecutionTickCount">the total number of ticks spent executing the previous event to be deducted from the current wait time</param>
        public virtual bool WaitNext(long lastExecutionTickCount)
        {
            //Thread.Sleep((int)Math.Max(0, this.Interval - lastExecutionTickCount / TimeSpan.TicksPerMillisecond));
            return OnWait(OnGetWaitTime(lastExecutionTickCount));
        }

        protected virtual DateTime OnGetWaitTime(long lastExecutionTickCount)
        {
            long ms = lastExecutionTickCount / TimeSpan.TicksPerMillisecond;
            if (Interval <= 0 || ms >= Interval) return DateTime.MinValue;

            DateTime now = CurrentTime.Now;
            long elapsed = (long)now.Subtract(DateRange.Start).TotalMilliseconds - ms;
            long normalized = (elapsed / Interval) * Interval; // cuts off fractional remainders, and normalizes to start time
            long intervals = normalized / Interval;
            intervals++;
            long add = intervals * Interval;
            DateTime next = DateRange.Start.AddMilliseconds(add);
            //TimeSpan wait = next.Subtract(CurrentTime.Now);
            //if (wait.TotalMilliseconds < 0)
            //    return TimeSpan.FromMilliseconds(0);
            return next;
        }

        
        protected virtual bool OnWait(DateTime expires)
        {
            bool ret = !IsCanceled;
            if (ret && Interval > 0 && expires > DateTime.MinValue)
            {
                _tpw.Reset();
                _tpw.TargetWaitTime = expires.Subtract(CurrentTime.Now);
                ret = _tpw.WaitOne(expires);
            }
            return !this.IsCanceled && ret;
        }

        /// <summary>
        /// Returns a bool indicating whether the schedule is no longer valid for the current date/time
        /// </summary>
        public bool IsExpired { get { return IsCanceled || !this[CurrentTime.Now].DateRange.Contains(CurrentTime.Now); } }
        public bool IsCanceled { get; set; }
    }

    public class PeriodicSchedule : Schedule
    {
        /// <summary>
        /// Creates a simple periodic scheduled event that executes on regular timed interval for the duration of the schedule
        /// </summary>
        /// <param name="dateRange">Duration for the schedule to run</param>
        /// <param name="type">type of periodic schedule</param>
        /// <param name="interval">interval in milliseconds</param>
        public PeriodicSchedule(DateRange dateRange, int interval) 
            : base(dateRange, IntervalType.Periodic) 
        { Interval = (int)System.Math.Min(interval, DateRange.Duration.TotalMilliseconds); }

        public static Schedule FromMilliseconds(int interval)
        {
            return new PeriodicSchedule(DateRange.Forever, interval);
        }

        public static Schedule FromSeconds(int interval)
        {
            return new PeriodicSchedule(DateRange.Forever, interval * 1000);
        }

        public static Schedule FromMinutes(int interval)
        {
            return new PeriodicSchedule(DateRange.Forever, interval* 60 * 1000);
        }

        public static Schedule FromHours(int interval)
        {
            return new PeriodicSchedule(DateRange.Forever, interval * 60 * 60 * 1000);
        }

        public static Schedule FromDays(int interval)
        {
            return new PeriodicSchedule(DateRange.Forever, interval * 24 * 60 * 60 * 1000);
        }

        public static Schedule FromWeeks(int interval)
        {
            return new PeriodicSchedule(DateRange.Forever, interval * 7 * 24 * 60 * 1000);
        }
    }

    public enum CalendricalScheduleType
    {
        Fixed,
        HourlyRecurring,
        DailyRecurring,
        WeeklyRecurring,
        MonthlyRecurring,
        YearlyRecurring
    }
    public class CalendricalSchedule : Schedule
    {
        public CalendricalSchedule(DateRange dateRange, CalendricalScheduleType type, int interval, TimeSpan eventDuration) 
            : base(dateRange, IntervalType.Calendrical) 
        { 
            TimeSpan maxDuration = TimeSpan.MaxValue;

            switch (type)
            {
                case Scheduling.CalendricalScheduleType.HourlyRecurring:
                    {
                        maxDuration = TimeSpan.FromHours(interval);
                        break;
                    }
                case Scheduling.CalendricalScheduleType.DailyRecurring:
                    {
                        maxDuration = TimeSpan.FromDays(interval);
                        break;
                    }
                case Scheduling.CalendricalScheduleType.WeeklyRecurring:
                    {
                        maxDuration = TimeSpan.FromDays(interval * 7);
                        break;
                    }
                case Scheduling.CalendricalScheduleType.MonthlyRecurring:
                    {
                        maxDuration = TimeSpan.FromDays(interval * 31);
                        break;
                    }
                case Scheduling.CalendricalScheduleType.YearlyRecurring:
                    {
                        maxDuration = TimeSpan.FromDays(interval * 365);
                        break;
                    }
            }
            EventDuration = eventDuration.TotalMilliseconds > maxDuration.TotalMilliseconds ? maxDuration : eventDuration;
            Interval = (long)maxDuration.TotalMilliseconds;
        }
        public DateRange EventDateRange { get; set; }
        public CalendricalScheduleType CalendricalScheduleType { get; set; }    
    }

    public class FixedCalendricalSchedule : CalendricalSchedule
    {
        public FixedCalendricalSchedule(DateRange dateRange, int interval, TimeSpan eventDuration) 
            : base(dateRange, CalendricalScheduleType.Fixed, interval, eventDuration) { }

        public override bool IsScheduled(long ticks)
        {
            bool baseScheduled = base.IsScheduled(ticks);
            bool thisScheduled = false;
            if (baseScheduled)
            {
                long endTicks = DateRange.Start.Add(EventDuration).Ticks;
                thisScheduled = DateRange.Start.Ticks <= ticks && endTicks >= ticks;
            }
            return baseScheduled && thisScheduled;
        }
    }

    public class HourlyRecurringCalendricalSchedule : CalendricalSchedule
    {
        public HourlyRecurringCalendricalSchedule(DateRange dateRange, int interval, TimeSpan eventDuration)
            : base(dateRange, CalendricalScheduleType.HourlyRecurring, interval, eventDuration) 
        {
            HourlyInterval = (long)TimeSpan.FromMilliseconds(Interval).TotalHours;
        }

        public long HourlyInterval { get; private set; }

        public override bool IsScheduled(long ticks)
        {
            bool baseScheduled = base.IsScheduled(ticks);
            bool thisScheduled = false;
            if (baseScheduled)
            {
                int interval = 0;
                long endTicks = DateRange.Start.Add(EventDuration).Ticks;
                long startTicks = DateRange.Start.Ticks;
                while (endTicks < DateRange.End.Ticks)
                {
                    interval++;
                    if (ticks < startTicks) break;
                    thisScheduled = startTicks <= ticks && endTicks >= ticks;
                    if (thisScheduled)
                        break;
                    else
                    {
                        startTicks = DateRange.Start.Add(TimeSpan.FromHours(HourlyInterval * interval)).Ticks;
                        endTicks = startTicks + EventDuration.Ticks;
                    }
                }
            }
            return baseScheduled && thisScheduled;
        }

        

        protected override DateTime OnGetWaitTime(long lastExecutionTickCount)
        {
            DateTime now = CurrentTime.Now;
            long elapsed = (long)now.Subtract(DateRange.Start).TotalMilliseconds - lastExecutionTickCount;
            long normalized = (elapsed / (1000 * 60 * 60)) * 1000 * 60 * 60; // cuts off fractional remainders, and normalizes to start time
            long intervals = normalized / Interval;
            intervals++;
            long add = intervals * Interval;
            DateTime next = DateRange.Start.AddMilliseconds(add);
            return next;
        }
    }


    public class DailyRecurringCalendricalSchedule : CalendricalSchedule
    {
        public DailyRecurringCalendricalSchedule(DateRange dateRange, int interval, TimeSpan eventDuration, int dailyInterval) 
            : base(dateRange, CalendricalScheduleType.DailyRecurring, interval, eventDuration) 
        {
            DailyInterval = dailyInterval;
        }
        public int DailyInterval { get; set; }

        public override bool IsScheduled(long ticks)
        {
            bool baseScheduled = base.IsScheduled(ticks);
            bool thisScheduled = false;
            if (baseScheduled)
            {
                int interval = 0;
                long endTicks = DateRange.Start.Add(EventDuration).Ticks;
                long startTicks = DateRange.Start.Ticks;
                while (endTicks < DateRange.End.Ticks)
                {
                    interval++;
                    if (ticks < startTicks) break;
                    thisScheduled = startTicks <= ticks && endTicks >= ticks;
                    if (thisScheduled)
                        break;
                    else
                    {
                        startTicks = DateRange.Start.Add(TimeSpan.FromDays(DailyInterval * interval)).Ticks;
                        endTicks = startTicks + EventDuration.Ticks;
                    }
                }
            }
            return baseScheduled && thisScheduled;
        }
    }
    [Flags()]
    public enum Days
    {
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
        Sunday = 1,
        EveryDay = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
        Weekends = Saturday | Sunday,
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday
    }
    public class WeeklyRecurringCalendricalSchedule : CalendricalSchedule
    {
        public WeeklyRecurringCalendricalSchedule(DateRange dateRange, int interval, TimeSpan eventDuration, Days daysOfWeek, int weeklyInterval) 
            : base(dateRange, CalendricalScheduleType.WeeklyRecurring, interval, eventDuration) 
        {
            DaysOfWeek = daysOfWeek;
            WeeklyInterval = weeklyInterval;
        }
        public int WeeklyInterval { get; set; }
        public Days DaysOfWeek { get; set; }

        public override bool IsScheduled(long ticks)
        {
            bool baseScheduled = base.IsScheduled(ticks);
            bool thisScheduled = false;
            if (baseScheduled)
            {
                Days[] days = this.DaysOfWeek.GetFlags().OfType<Days>().ToArray();
                int interval = 0;
                long endTicks = 0;
                long startTicks = DateRange.Start.Ticks;
                while (endTicks < DateRange.End.Ticks)
                {
                    foreach (Days day in days)
                    {
                        startTicks = GetTicksForDay(day, startTicks);
                        endTicks = startTicks + EventDuration.Ticks;
                        if (ticks < startTicks) break;
                        thisScheduled = startTicks <= ticks && endTicks >= ticks;
                        if (thisScheduled)
                            break;
                    }
                    interval++;
                    startTicks = DateRange.Start.Add(TimeSpan.FromDays(WeeklyInterval * interval * 7)).Ticks;
                }
            }
            return baseScheduled && thisScheduled;
        }

        private long GetTicksForDay(Days day, long startTicks)
        {
            DateTime current = new DateTime().AddTicks(startTicks);
            int dayOfWeek = (int)System.Math.Log((int)day, 2) - 1;
            int dayDelta = (int)current.DayOfWeek - dayOfWeek;
            current.AddDays(dayDelta);
            return current.Ticks;
        }
    }

    public class MonthlyRecurringCalendricalSchedule : CalendricalSchedule
    {
        public MonthlyRecurringCalendricalSchedule(DateRange dateRange, int interval, TimeSpan eventDuration) 
            : base(dateRange, CalendricalScheduleType.MonthlyRecurring, interval, eventDuration) { }
        public byte DayOfMonth { get; set; }
        public byte MonthlyInterval { get; set; }

        public override bool IsScheduled(long ticks)
        {
            bool baseScheduled = base.IsScheduled(ticks);
            bool thisScheduled = false;
            if (baseScheduled)
            {
                int interval = 0;
                
                long startTicks = GetTicksForDay(DayOfMonth, DateRange.Start.Ticks);
                long endTicks = startTicks + EventDuration.Ticks;
                while (endTicks < DateRange.End.Ticks)
                {
                    if (ticks < startTicks) break;
                    thisScheduled = startTicks <= ticks && endTicks >= ticks;
                    if (thisScheduled)
                        break;
                    else
                    {
                        interval++;
                        startTicks = GetTicksForDay(DayOfMonth, new DateTime().AddTicks(DateRange.Start.Ticks).AddMonths(MonthlyInterval * interval).Ticks);
                        endTicks = startTicks + EventDuration.Ticks;
                    }
                }
            }
            return baseScheduled && thisScheduled;
        }

        private long GetTicksForDay(byte dayOfMonth, long startTicks)
        {
            DateTime current = new DateTime().AddTicks(startTicks);
            int dayDelta = (int)current.Day - dayOfMonth;
            current.AddDays(dayDelta);
            return current.Ticks;
        }
    }
    [Flags()]
    public enum Months
    {
        January = 1,
        February = 2,
        March = 4,
        April = 8,
        May = 16,
        June = 32,
        July = 64,
        August = 128,
        September = 256,
        October = 512,
        November = 1024,
        December = 2048
    }
    public class YearkyRecurringCalendricalSchedule : CalendricalSchedule
    {
        public YearkyRecurringCalendricalSchedule(DateRange dateRange, int interval, TimeSpan eventDuration) 
            : base(dateRange, CalendricalScheduleType.YearlyRecurring, interval, eventDuration) { }
        public Months MonthsOfYear { get; set; }
        public byte DayOfMonth { get; set; }
        public byte YearlyInterval { get; set; }

        public override bool IsScheduled(long ticks)
        {
            bool baseScheduled = base.IsScheduled(ticks);
            bool thisScheduled = false;
            if (baseScheduled)
            {
                Months[] months = this.MonthsOfYear.GetFlags().OfType<Months>().ToArray();
                int interval = 0;
                long endTicks = 0;
                long startTicks = DateRange.Start.Ticks;
                while (endTicks < DateRange.End.Ticks)
                {
                    foreach (Months month in months)
                    {
                        startTicks = GetTicksForDayOfMonth(DayOfMonth, month, startTicks);
                        endTicks = startTicks + EventDuration.Ticks;
                        if (ticks < startTicks) break;
                        thisScheduled = startTicks <= ticks && endTicks >= ticks;
                        if (thisScheduled)
                            break;
                    }
                    interval++;
                    startTicks = new DateTime().AddTicks(DateRange.Start.Ticks).AddYears(YearlyInterval * interval).Ticks;
                }
            }
            return baseScheduled && thisScheduled;
        }

        private long GetTicksForDayOfMonth(byte dayOfMonth, Months month, long startTicks)
        {
            DateTime current = new DateTime().AddTicks(startTicks);
            int dayDelta = (int)current.DayOfWeek - dayOfMonth;
            int theMonth = (int)System.Math.Log((int)month, 2);
            int monthDelta = current.Month - theMonth;
            current.AddMonths(monthDelta);
            current.AddDays(dayDelta);
            return current.Ticks;
        }
    }
}
