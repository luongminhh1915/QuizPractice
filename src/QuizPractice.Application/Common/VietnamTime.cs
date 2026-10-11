namespace QuizPractice.Application.Common;

/// <summary>
/// The days the organization works in. The database keeps UTC; a day on a screen is a
/// Vietnam day (UTC+7, no daylight saving), so an order at 06:00 local time is not
/// counted on the previous day.
/// </summary>
public static class VietnamTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(utcNow + Offset);

    public static DateOnly DayOf(DateTime utc) => DateOnly.FromDateTime(utc + Offset);

    /// <summary>The UTC instant a Vietnam day starts.</summary>
    public static DateTime StartUtc(DateOnly day) => day.ToDateTime(TimeOnly.MinValue) - Offset;
}
