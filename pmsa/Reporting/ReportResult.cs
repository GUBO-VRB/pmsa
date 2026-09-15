namespace pmsa.Reporting;

/// <summary>
/// One project's share of a report (spec 012, Section 5.3).
/// </summary>
/// <remarks>
/// <c>Hours</c> and <c>Share</c> are derived from <see cref="Minutes"/>, never stored — the
/// <em>rounding happens once, at the end</em> invariant.
/// </remarks>
/// <param name="Key">The project's id, or <see cref="OtherKey"/> for the collected tail of FR-012.</param>
/// <param name="Label">The project's name, resolved at read time (EC-12).</param>
/// <param name="Client">
/// FR-009's client column. A label only; null when the project has no client, and null for the
/// <em>Other</em> slice. Spec 013's non-project dimensions will leave it null too.
/// </param>
/// <param name="Minutes">Always greater than zero — a zero slice is omitted entirely (FR-011).</param>
public sealed record ReportSlice(Guid Key, string Label, string? Client, int Minutes)
{
    /// <summary>The <em>Other</em> slice is a presentation grouping, not a project.</summary>
    public static readonly Guid OtherKey = Guid.Empty;

    public bool IsOther => Key == OtherKey;

    public decimal Hours => ReportRounding.ToHours(Minutes);

    public decimal ShareOf(int totalMinutes) => ReportRounding.ToShare(Minutes, totalMinutes);
}

/// <summary>
/// The answer to a <see cref="ReportQuery"/> (spec 012, Section 5.3).
/// </summary>
/// <param name="Query">The question this answers.</param>
/// <param name="Slices">Every project with hours in range, ordered by minutes descending then label ascending.</param>
/// <param name="TotalMinutes">Equals the sum of the slices' minutes exactly — the <em>slices reconcile</em> invariant.</param>
public sealed record ReportResult(ReportQuery Query, IReadOnlyList<ReportSlice> Slices, int TotalMinutes)
{
    /// <summary>FR-012: the chart shows at most this many project bars before collecting a tail.</summary>
    public const int MaxChartSlices = 15;

    public static ReportResult Empty(ReportQuery query) => new(query, [], 0);

    public bool IsEmpty => Slices.Count == 0;

    /// <summary>FR-010. Derived from the total minutes, never by adding rounded slice hours.</summary>
    public decimal TotalHours => ReportRounding.ToHours(TotalMinutes);

    /// <summary>
    /// FR-012. At most <see cref="MaxChartSlices"/> projects are charted; anything beyond that is
    /// collected into one <em>Other (N projects)</em> bar. Fewer than 16 projects means no
    /// <em>Other</em> bar at all (EC-8), and exactly 16 means 15 bars plus <em>Other (1 project)</em>
    /// — the rule is not relaxed for a tail of one (EC-7). The table is built from
    /// <see cref="Slices"/> and always lists every project.
    /// </summary>
    public IReadOnlyList<ReportSlice> ChartSlices
    {
        get
        {
            if (Slices.Count <= MaxChartSlices)
            {
                return Slices;
            }

            var tail = Slices.Skip(MaxChartSlices).ToList();
            var label = $"Other ({tail.Count} project{(tail.Count == 1 ? string.Empty : "s")})";

            return
            [
                .. Slices.Take(MaxChartSlices),
                new ReportSlice(ReportSlice.OtherKey, label, Client: null, tail.Sum(slice => slice.Minutes)),
            ];
        }
    }

    /// <summary>
    /// FR-023 and EC-10: the displayed percentages are each rounded to one decimal, so across
    /// many slices they can land on 99.9 or 100.1. The <em>hours</em> always reconcile exactly
    /// (NFR-004); only the shares may not, and when they do not the screen says so.
    /// </summary>
    public bool SharesAreRounded =>
        !IsEmpty && Slices.Sum(slice => slice.ShareOf(TotalMinutes)) != 100.0m;
}

/// <summary>
/// The one place minutes become the numbers a person reads (spec 001, <c>Duration</c>).
/// </summary>
/// <remarks>
/// Everything upstream of here is integer minutes. Totals are computed by summing minutes and
/// converting once, never by summing rounded hours — otherwise three 10-minute entries display
/// as 0.50 while totalling 0.51.
/// </remarks>
public static class ReportRounding
{
    /// <summary>Decimal hours to two places, half away from zero.</summary>
    public static decimal ToHours(int minutes) =>
        Math.Round(minutes / 60m, 2, MidpointRounding.AwayFromZero);

    /// <summary>A percentage to one place, computed from whole minutes (FR-023).</summary>
    public static decimal ToShare(int minutes, int totalMinutes) =>
        totalMinutes == 0 ? 0m : Math.Round(minutes * 100m / totalMinutes, 1, MidpointRounding.AwayFromZero);
}
