namespace pmsa.Reporting;

/// <summary>
/// The whole question a report answers, and exactly what FR-018 carries in the address
/// (spec 012, Section 5.3).
/// </summary>
/// <remarks>
/// A ReportQuery is <strong>not</strong> a permission. The viewer's role is applied on top of it
/// on every request (FR-019, and the <em>scoping is applied server-side on every request</em>
/// invariant), which is what makes it safe to put in a shareable address.
/// </remarks>
/// <param name="Range">Required.</param>
/// <param name="PersonIds">Empty means <em>all the role permits</em>. Ignored for the User role.</param>
/// <param name="ProjectIds">Empty means <em>all the role permits</em>.</param>
public sealed record ReportQuery(
    DateRange Range,
    IReadOnlySet<Guid> PersonIds,
    IReadOnlySet<Guid> ProjectIds)
{
    public static ReportQuery Unfiltered(DateRange range) =>
        new(range, new HashSet<Guid>(), new HashSet<Guid>());
}
