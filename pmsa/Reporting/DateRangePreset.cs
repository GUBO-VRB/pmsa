namespace pmsa.Reporting;

/// <summary>The named ranges of FR-003.</summary>
public enum DateRangePreset
{
    /// <summary>FR-003: the range the report screen opens on.</summary>
    ThisMonth = 0,
    LastMonth = 1,
    ThisWeek = 2,
    LastWeek = 3,
    ThisQuarter = 4,
    ThisYear = 5,

    /// <summary>Begin and end are whatever the person typed.</summary>
    Custom = 6,
}

public static class DateRangePresets
{
    /// <summary>FR-003. The order here is the order the control offers them in.</summary>
    public static readonly IReadOnlyList<DateRangePreset> All =
    [
        DateRangePreset.ThisWeek,
        DateRangePreset.LastWeek,
        DateRangePreset.ThisMonth,
        DateRangePreset.LastMonth,
        DateRangePreset.ThisQuarter,
        DateRangePreset.ThisYear,
        DateRangePreset.Custom,
    ];

    public const DateRangePreset Default = DateRangePreset.ThisMonth;

    public static string Label(this DateRangePreset preset) => preset switch
    {
        DateRangePreset.ThisWeek => "This week",
        DateRangePreset.LastWeek => "Last week",
        DateRangePreset.ThisMonth => "This month",
        DateRangePreset.LastMonth => "Last month",
        DateRangePreset.ThisQuarter => "This quarter",
        DateRangePreset.ThisYear => "This year",
        _ => "Custom",
    };

    /// <summary>
    /// Turns a preset into the range it names, relative to <paramref name="today"/>. Returns null
    /// for <see cref="DateRangePreset.Custom"/>, which has no range of its own.
    /// </summary>
    /// <remarks>
    /// Every week-shaped preset goes through <see cref="StartOfWeek"/>, so FR-004's Monday start
    /// is stated once. A quarter or a year that has not finished still reports to its last day —
    /// the future days simply contribute nothing (EC-2), which is cheaper to explain than a range
    /// that silently stops at today.
    /// </remarks>
    public static DateRange? Resolve(this DateRangePreset preset, DateOnly today) => preset switch
    {
        DateRangePreset.ThisWeek => Week(StartOfWeek(today)),
        DateRangePreset.LastWeek => Week(StartOfWeek(today).AddDays(-7)),
        DateRangePreset.ThisMonth => Month(today.Year, today.Month),
        DateRangePreset.LastMonth => Month(today.AddMonths(-1).Year, today.AddMonths(-1).Month),
        DateRangePreset.ThisQuarter => Quarter(today),
        DateRangePreset.ThisYear => DateRange.Create(new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31)),
        _ => null,
    };

    /// <summary>
    /// FR-004: a week starts on Monday (ISO-8601), wherever a week is a preset or a bucket.
    /// Story 013's Time dimension buckets on this same function.
    /// </summary>
    public static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>
    /// The preset a range corresponds to, or <see cref="DateRangePreset.Custom"/> if it matches
    /// none. This is what lets a shared address (FR-018) carrying only two dates come back with
    /// the right radio selected (SC-002).
    /// </summary>
    public static DateRangePreset Classify(DateRange range, DateOnly today) =>
        All.FirstOrDefault(
            preset => preset.Resolve(today) is { } resolved && resolved == range,
            DateRangePreset.Custom);

    private static DateRange Week(DateOnly monday) => DateRange.Create(monday, monday.AddDays(6));

    private static DateRange Month(int year, int month) =>
        DateRange.Create(new DateOnly(year, month, 1), new DateOnly(year, month, DateTime.DaysInMonth(year, month)));

    private static DateRange Quarter(DateOnly today)
    {
        var firstMonth = ((today.Month - 1) / 3 * 3) + 1;
        var begin = new DateOnly(today.Year, firstMonth, 1);
        return DateRange.Create(begin, begin.AddMonths(3).AddDays(-1));
    }
}
