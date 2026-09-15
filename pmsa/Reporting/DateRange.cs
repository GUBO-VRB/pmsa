namespace pmsa.Reporting;

/// <summary>
/// An inclusive span of calendar days (spec 012, Section 5.3).
/// </summary>
/// <remarks>
/// Inclusive on both ends: <c>2026-09-01</c>–<c>2026-09-01</c> is one day, not zero (EC-1).
/// </remarks>
public sealed record DateRange
{
    /// <summary>FR-006. The boundary is inclusive: 366 days is accepted, 367 refused (EC-17).</summary>
    public const int MaxDays = 366;

    public const string EndBeforeBeginMessage = "The end date must not precede the begin date.";

    public static readonly string TooLongMessage = $"A report covers at most {MaxDays} days.";

    private DateRange(DateOnly begin, DateOnly end)
    {
        Begin = begin;
        End = end;
    }

    public DateOnly Begin { get; }

    public DateOnly End { get; }

    /// <summary>Length in days, counting both ends.</summary>
    public int Days => End.DayNumber - Begin.DayNumber + 1;

    /// <summary>
    /// FR-005 and FR-006. Returns null and a message rather than throwing, because both refusals
    /// are things a person is told (SC-005, SC-006) rather than faults.
    /// </summary>
    public static DateRange? TryCreate(DateOnly begin, DateOnly end, out string? problem)
    {
        if (end < begin)
        {
            problem = EndBeforeBeginMessage;
            return null;
        }

        if (end.DayNumber - begin.DayNumber + 1 > MaxDays)
        {
            problem = TooLongMessage;
            return null;
        }

        problem = null;
        return new DateRange(begin, end);
    }

    /// <summary>For ranges already known to be well formed, such as those a preset produces.</summary>
    public static DateRange Create(DateOnly begin, DateOnly end) =>
        TryCreate(begin, end, out var problem) ?? throw new ArgumentException(problem, nameof(end));
}
