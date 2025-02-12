using System;

namespace OllamaClientLibrary.Extensions
{
    internal static class DateTimeExtensions
    {
        public static DateTime? AsDateTime(this string relativeTime)
        {
            var now = DateTime.UtcNow;
            var parts = relativeTime.Split(' ');

            if (relativeTime.ToLower() == "yesterday")
            {
                return now.AddDays(-1);
            }

            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], out int value))
                {
                    var unit = parts[1].ToLower();
                    switch (unit)
                    {
                        case "second":
                        case "seconds":
                            return now.AddSeconds(-value);
                        case "minute":
                        case "minutes":
                            return now.AddMinutes(-value);
                        case "hour":
                        case "hours":
                            return now.AddHours(-value);
                        case "day":
                        case "days":
                            return now.AddDays(-value);
                        case "week":
                        case "weeks":
                            return now.AddDays(-value * 7);
                        case "month":
                        case "months":
                            return now.AddMonths(-value);
                        case "year":
                        case "years":
                            return now.AddYears(-value);
                        default:
                            return null;
                    }
                }
            }

            return null;
        }
    }
}
