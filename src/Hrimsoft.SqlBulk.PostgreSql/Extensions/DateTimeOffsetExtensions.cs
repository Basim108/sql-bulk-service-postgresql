using System;

namespace Hrimsoft.SqlBulk.PostgreSql
{
    /// <summary>
    /// Methods that extend DateTimeOffset functionality
    /// </summary>
    internal static class DateTimeOffsetExtensions
    {
        /// <summary>
        /// Truncate DateTimeOffset to milliseconds, seconds, etc that is depends on TimeSpan 
        /// </summary>
        /// <param name="dateTime">date and time that must be truncated</param>
        /// <param name="timeSpan">a period to which it will truncate. If it is needed to cut all micro and nano seconds Time span should be 1 millisecond</param>
        public static DateTimeOffset Truncate(this DateTimeOffset dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
                return dateTime;
            if (dateTime == DateTimeOffset.MinValue || dateTime == DateTimeOffset.MaxValue) 
                return dateTime; 
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
        
        /// <summary>
        /// Cut micro and nano seconds from DateTimeOffset 
        /// </summary>
        /// <param name="dateTime">date and time that must be cut</param>
        public static DateTimeOffset TruncateToMilliseconds(this DateTimeOffset dateTime)
        {
            return dateTime.Truncate(TimeSpan.FromMilliseconds(1));
        }
        
        /// <summary>
        /// Cut micro-, nano- and milli- seconds from DateTimeOffset 
        /// </summary>
        /// <param name="dateTime">date and time that must be cut</param>
        public static DateTimeOffset TruncateToSeconds(this DateTimeOffset dateTime)
        {
            return dateTime.Truncate(TimeSpan.FromSeconds(1));
        }
    }
}