using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core
{
    public static class DateTimeEx
    {
        public static string ToJS(this System.DateTime input)
        {
            System.TimeSpan span = new System.TimeSpan(System.DateTime.Parse("1/1/1970").Ticks);
            System.DateTime time = input.Subtract(span);
            TimeSpan utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(CurrentTime.Now);
            return "\"/Date(" 
                + (long)(time.Ticks / 10000)
                + (utcOffset.TotalMilliseconds > 0 ? "+" : "") 
                + utcOffset.Hours.ToString("00") 
                + utcOffset.Minutes.ToString("00")
                + ")/\"";
        }

        public static bool IsBetween(this System.DateTime dt, System.DateTime start, System.DateTime end)
        {
            return dt >= start && dt <= end;
        }
    }
}
