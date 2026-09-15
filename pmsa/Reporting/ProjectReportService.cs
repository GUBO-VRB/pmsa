using Microsoft.EntityFrameworkCore;
using pmsa.Data;
using pmsa.Domain;

namespace pmsa.Reporting;

/// <summary>
/// What the signed-in person is permitted to see, independent of what they asked for.
/// </summary>
/// <param name="ViewerId">The signed-in Person.</param>
/// <param name="ViewerRole">Decides the scope — spec 007, FR-011 and FR-012.</param>
public readonly record struct ReportScope(Guid ViewerId, Role ViewerRole)
{
    /// <summary>FR-014: Manager and Admin report across everybody's entries.</summary>
    public bool SeesEveryone => ViewerRole is Role.Manager or Role.Admin;
}

/// <param name="Id">The person or project this option selects.</param>
/// <param name="Label">What the control shows.</param>
/// <param name="IsActive">
/// False for a deactivated person or an archived project. Both stay selectable — their history is
/// the reason to select them (FR-017, EC-18).
/// </param>
public sealed record FilterOption(Guid Id, string Label, bool IsActive);

/// <param name="People">Empty for the User role, whose person filter is not offered at all (FR-015).</param>
public sealed record ReportFilterOptions(IReadOnlyList<FilterOption> People, IReadOnlyList<FilterOption> Projects);

/// <summary>
/// The report of spec 012: a read-only query over <see cref="TimeEntry"/>, grouped by project.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here writes, and nothing here stores an aggregate (FR-021, FR-022). Every figure is
/// recomputed from the entries on every request, which is what makes a report incapable of
/// disagreeing with the entries beneath it.
/// </para>
/// <para>
/// Scoping is applied inside the query that fetches the entries rather than by filtering after
/// retrieval (NFR-005), so no unpermitted hour is ever loaded into a response.
/// </para>
/// </remarks>
public class ProjectReportService(PmsaDbContext db)
{
    /// <summary>
    /// Runs <paramref name="query"/> under <paramref name="scope"/>. The scope always wins: a
    /// filter narrows, never widens.
    /// </summary>
    public async Task<ReportResult> RunAsync(
        ReportQuery query, ReportScope scope, CancellationToken cancellationToken = default)
    {
        var entries = db.TimeEntries
            .AsNoTracking()
            // Membership is by date, not by when it was typed — never CreatedAt. Inclusive on
            // both ends (FR-002). Deactivated people and archived projects are not excluded here
            // or anywhere else (FR-017).
            .Where(e => e.Date >= query.Range.Begin && e.Date <= query.Range.End);

        if (scope.SeesEveryone)
        {
            if (query.PersonIds.Count > 0)
            {
                entries = entries.Where(e => query.PersonIds.Contains(e.PersonId));
            }
        }
        else
        {
            // FR-013. Applied instead of, not after, the requested person filter — which is why a
            // shared address carrying someone else's id can only ever resolve to the viewer
            // themselves (EC-6).
            entries = entries.Where(e => e.PersonId == scope.ViewerId);
        }

        if (query.ProjectIds.Count > 0)
        {
            entries = entries.Where(e => query.ProjectIds.Contains(e.ProjectId));
        }

        // The subtraction is written out rather than read from TimeEntry.DurationMinutes because
        // the derived property is unmapped; this form translates to SUM() in the store and keeps
        // NFR-003's aggregation off the application server.
        var totals = await entries
            .GroupBy(e => e.ProjectId)
            .Select(g => new { ProjectId = g.Key, Minutes = g.Sum(e => e.EndMinutes - e.BeginMinutes) })
            .ToListAsync(cancellationToken);

        // FR-011: a project with no hours in the range is not a zero slice, it is absent. Every
        // group here holds at least one entry and every entry lasts at least a minute, so the
        // guard only ever fires on data that broke spec 001's "a range moves forward" rule.
        totals = totals.Where(t => t.Minutes > 0).ToList();

        if (totals.Count == 0)
        {
            return ReportResult.Empty(query);
        }

        var projectIds = totals.Select(t => t.ProjectId).ToList();
        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var slices = totals
            .Select(t => new ReportSlice(
                t.ProjectId,
                projects.TryGetValue(t.ProjectId, out var project) ? project.Name : "(unknown project)",
                projects.TryGetValue(t.ProjectId, out var withClient) ? withClient.Client : null,
                t.Minutes))
            // FR-008: hours descending, ties broken by project name ascending (SC-004). Two
            // projects sharing a name stay two slices, keyed by id (EC-14).
            .OrderByDescending(slice => slice.Minutes)
            .ThenBy(slice => slice.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new ReportResult(query, slices, slices.Sum(slice => slice.Minutes));
    }

    /// <summary>
    /// The contents of the two filter controls (FR-015, FR-016).
    /// </summary>
    /// <remarks>
    /// Every project is offered to everybody. Roles are global and project assignment (story 009)
    /// governs what a person may <em>book to</em>, not what they may report on — so there is no
    /// project a signed-in person is forbidden to name. When story 009 lands, this is the method
    /// that would narrow the list, and EC-5 already describes what happens to an id that falls
    /// outside it.
    /// </remarks>
    public async Task<ReportFilterOptions> LoadFilterOptionsAsync(
        ReportScope scope, CancellationToken cancellationToken = default)
    {
        var projects = await db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new FilterOption(p.Id, p.Name, p.IsActive))
            .ToListAsync(cancellationToken);

        // FR-015: the control is omitted entirely for the User role, so its contents are never
        // fetched either (SC-010).
        var people = scope.SeesEveryone
            ? await db.People
                .AsNoTracking()
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.FullName)
                .Select(p => new FilterOption(p.Id, p.FullName, p.IsActive))
                .ToListAsync(cancellationToken)
            : [];

        return new ReportFilterOptions(people, projects);
    }

    /// <summary>
    /// EC-15: distinguishes "this range is empty" from "this person has never logged anything",
    /// so the empty state can point at story 009 rather than showing a bare chart.
    /// </summary>
    public Task<bool> HasAnyEntriesAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.TimeEntries.AsNoTracking().AnyAsync(e => e.PersonId == personId, cancellationToken);
}
