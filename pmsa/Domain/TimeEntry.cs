namespace pmsa.Domain;

/// <summary>
/// A contiguous block of a person's working day spent on one project — the atom every report
/// aggregates (spec 001, Section 5.1).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Owned by spec 001</strong>, which is not built yet. This is the minimal shape
/// [spec 012](../docs/specs/012-date-range-project-report.md) reads: <c>personId</c>,
/// <c>projectId</c>, <c>date</c> and the derived duration. Spec 001 owns the entry screen and the
/// rules that guard a write — no overlap, no midnight crossing, no future date. Nothing in spec
/// 012 writes a TimeEntry (<em>the report is read-only</em>).
/// </para>
/// <para>
/// The times are held as <strong>minutes from midnight</strong> rather than as
/// <see cref="TimeOnly"/> because spec 001 allows an end of <c>24:00</c>, which
/// <see cref="TimeOnly"/> cannot represent. Integer minutes also make spec 012's
/// <em>rounding happens once, at the end</em> invariant structural: the store sums minutes and
/// only the presentation layer divides by 60.
/// </para>
/// </remarks>
public class TimeEntry
{
    /// <summary>Midnight ending the entry's own date — the largest legal <see cref="EndMinutes"/>.</summary>
    public const int MinutesPerDay = 24 * 60;

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    /// <summary>The owner. Set from the session at creation and never reassigned (spec 007, FR-005).</summary>
    public Guid PersonId { get; private set; }

    public Guid ProjectId { get; private set; }

    /// <summary>
    /// The calendar day the hours belong to. A report selects on this and never on
    /// <see cref="CreatedAt"/> — the <em>membership is by date, not by when it was typed</em>
    /// invariant.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>Minutes from midnight, 0–1439.</summary>
    public int BeginMinutes { get; private set; }

    /// <summary>Minutes from midnight, 1–1440; strictly greater than <see cref="BeginMinutes"/>.</summary>
    public int EndMinutes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Reserved for story 005; unused while entries are create-only.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    public Person? Person { get; private set; }

    public Project? Project { get; private set; }

    /// <summary>
    /// Derived, never stored — storing it would let it disagree with the times. Expressed as a
    /// subtraction so that Entity Framework can translate <c>Sum(e =&gt; e.DurationMinutes)</c>
    /// into SQL and keep the aggregation in the store (spec 012, NFR-003).
    /// </summary>
    public int DurationMinutes => EndMinutes - BeginMinutes;

    private TimeEntry() { }

    /// <summary>
    /// Creates an entry from already-validated parts. Spec 001 owns the validation — the overlap
    /// check, the not-in-the-future check and the bookability check all need the store, so they
    /// belong to its service rather than here.
    /// </summary>
    public static TimeEntry Create(
        Guid personId, Guid projectId, DateOnly date, int beginMinutes, int endMinutes, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endMinutes, MinutesPerDay);

        // A range moves forward: a zero-length entry records nothing and is refused.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(beginMinutes, endMinutes);

        return new TimeEntry
        {
            PersonId = personId,
            ProjectId = projectId,
            Date = date,
            BeginMinutes = beginMinutes,
            EndMinutes = endMinutes,
            CreatedAt = now,
        };
    }
}
