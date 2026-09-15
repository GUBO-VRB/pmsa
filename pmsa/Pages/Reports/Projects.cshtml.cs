using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Domain;
using pmsa.Reporting;
using pmsa.Security;

namespace pmsa.Pages.Reports;

/// <summary>
/// The date-range project report of spec 012: pick a span of time, see how the hours divided
/// across projects, as a chart and as a table.
/// </summary>
/// <remarks>
/// <para>
/// Available to every signed-in person whatever their role (FR-001); the role decides what the
/// numbers cover, not whether the screen exists.
/// </para>
/// <para>
/// The screen is read-only (FR-022): it has no handler but <see cref="OnGetAsync"/>, so there is
/// no path through this feature that writes anything.
/// </para>
/// <para>
/// The address is the report (FR-018). Every GET canonicalises itself to an explicit
/// <c>from</c>/<c>to</c> pair, so that copying the address out of the bar always yields a link
/// that reproduces the same range rather than one that drifts with the calendar (SC-014).
/// </para>
/// </remarks>
public class ProjectsModel(ProjectReportService reports, TimeProvider clock) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "preset")]
    public string? PresetInput { get; set; }

    [BindProperty(SupportsGet = true, Name = "from")]
    public string? FromInput { get; set; }

    [BindProperty(SupportsGet = true, Name = "to")]
    public string? ToInput { get; set; }

    /// <summary>
    /// Bound as text rather than as <see cref="Guid"/> so that a malformed id in a hand-edited or
    /// stale address takes EC-5's path — dropped from the filter — instead of failing the request.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "person")]
    public string[] PersonInput { get; set; } = [];

    [BindProperty(SupportsGet = true, Name = "project")]
    public string[] ProjectInput { get; set; } = [];

    public DateOnly Today { get; private set; }

    public DateRange? Range { get; private set; }

    public DateRangePreset Preset { get; private set; } = DateRangePresets.Default;

    public ReportFilterOptions Options { get; private set; } = new([], []);

    public IReadOnlySet<Guid> SelectedPersonIds { get; private set; } = new HashSet<Guid>();

    public IReadOnlySet<Guid> SelectedProjectIds { get; private set; } = new HashSet<Guid>();

    /// <summary>FR-015: absent for the User role, not merely disabled.</summary>
    public bool ShowPersonFilter { get; private set; }

    public ReportResult? Result { get; private set; }

    /// <summary>FR-005, FR-006 and EC-4 all land here. While set, no result was computed.</summary>
    public string? RangeProblem { get; private set; }

    /// <summary>EC-5: the viewer is told when a filter was narrowed, without disclosing why.</summary>
    public string? FilterNotice { get; private set; }

    /// <summary>EC-15: true when a User has never logged an entry at all, not merely none in range.</summary>
    public bool ViewerHasNoEntriesAtAll { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var cancellationToken = HttpContext.RequestAborted;

        // Wall-clock local dates with no timezone handling, inherited from spec 001, Section 9.2.
        Today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

        var scope = new ReportScope(
            User.GetPersonId() ?? Guid.Empty,
            User.GetRole() ?? Role.User);

        ShowPersonFilter = scope.SeesEveryone;
        Options = await reports.LoadFilterOptionsAsync(scope, cancellationToken);

        // A named preset is resolved server-side and then redirected away, so that the address
        // left behind carries the dates it produced rather than a name that means something
        // different tomorrow.
        if (Enum.TryParse<DateRangePreset>(PresetInput, ignoreCase: true, out var requested)
            && requested.Resolve(Today) is { } presetRange)
        {
            return RedirectToRange(presetRange);
        }

        var (range, rangeProblem, filtersAreTrustworthy) = ResolveRange();
        RangeProblem = rangeProblem;

        // EC-4: the whole query is refused when a date in the address is malformed — a partly
        // applied filter is never silently used, so the person and project ids go with it.
        if (filtersAreTrustworthy)
        {
            ResolveFilters(scope);
        }

        if (range is null)
        {
            // FR-005 and FR-006: no result is computed. Nothing replaces what is already on
            // screen, because the enhancement in the view never navigates away from a refused
            // range (SC-005); a refused range typed straight into the address has no previous
            // result to keep.
            return Page();
        }

        Range = range;
        Preset = DateRangePresets.Classify(range, Today);

        // Every report sits at an explicit address (FR-018). A bare /Reports/Projects becomes
        // this month's dates rather than staying a name. A refused address is left where it is,
        // since redirecting would throw away the message EC-4 owes the viewer.
        if (RangeProblem is null && !MatchesAddress(range))
        {
            return RedirectToRange(range);
        }

        var query = new ReportQuery(range, SelectedPersonIds, SelectedProjectIds);

        // A filter that asked for something and resolved to nothing means *nothing*, not
        // everything — the "a filter narrows, never widens" invariant. An empty id set is the
        // wire representation of "no filter", so this case cannot be handed to the query.
        Result = AskedForNothing()
            ? ReportResult.Empty(query)
            : await reports.RunAsync(query, scope, cancellationToken);

        // EC-15: only worth asking when there is nothing to show and the viewer is pinned to
        // their own hours.
        if (Result.IsEmpty && !scope.SeesEveryone)
        {
            ViewerHasNoEntriesAtAll = !await reports.HasAnyEntriesAsync(scope.ViewerId, cancellationToken);
        }

        return Page();
    }

    /// <summary>The address that reproduces the report currently on screen (FR-018).</summary>
    public string Permalink() =>
        Range is null ? Url.Page("/Reports/Projects")! : Url.Page("/Reports/Projects") + QueryStringFor(Range, SelectedPersonIds.Select(id => id.ToString()), SelectedProjectIds.Select(id => id.ToString()));

    /// <summary>
    /// Built by hand rather than through route values, because a filter is a <em>repeated</em>
    /// query parameter and a route-value dictionary would flatten the array to one string.
    /// </summary>
    private IActionResult RedirectToRange(DateRange range) =>
        Redirect(Url.Page("/Reports/Projects") + QueryStringFor(range, PersonInput, ProjectInput));

    private static string QueryStringFor(DateRange range, IEnumerable<string> personIds, IEnumerable<string> projectIds)
    {
        var parts = new List<string>
        {
            $"from={range.Begin:yyyy-MM-dd}",
            $"to={range.End:yyyy-MM-dd}",
        };

        parts.AddRange(personIds.Select(id => $"person={Uri.EscapeDataString(id)}"));
        parts.AddRange(projectIds.Select(id => $"project={Uri.EscapeDataString(id)}"));

        return "?" + string.Join('&', parts);
    }

    private (DateRange? Range, string? Problem, bool FiltersAreTrustworthy) ResolveRange()
    {
        var fromGiven = !string.IsNullOrWhiteSpace(FromInput);
        var toGiven = !string.IsNullOrWhiteSpace(ToInput);

        if (!fromGiven && !toGiven)
        {
            // FR-003: the screen opens on This month.
            return (DateRangePresets.Default.Resolve(Today), null, true);
        }

        if (!TryParseDate(FromInput, out var begin) || !TryParseDate(ToInput, out var end))
        {
            // EC-4. The default range is shown, the filter is discarded, and the person is told.
            FromInput = null;
            ToInput = null;
            PersonInput = [];
            ProjectInput = [];

            return (
                DateRangePresets.Default.Resolve(Today),
                "That report address carried a date that does not exist, so the default range is shown instead.",
                false);
        }

        var range = DateRange.TryCreate(begin, end, out var problem);
        return (range, problem, true);
    }

    private static bool TryParseDate(string? input, out DateOnly date) =>
        DateOnly.TryParseExact(input?.Trim(), "yyyy-MM-dd", out date);

    /// <summary>
    /// Intersects what was asked for with what is on offer. Ids that survive nothing are simply
    /// dropped and counted — EC-5 is explicit that existence is never disclosed by an error.
    /// </summary>
    private void ResolveFilters(ReportScope scope)
    {
        // FR-015 and EC-6: a User's person filter is ignored entirely, including one arriving in
        // a link shared by a Manager.
        var (people, droppedPeople) = scope.SeesEveryone
            ? Intersect(PersonInput, Options.People)
            : (new HashSet<Guid>(), 0);

        var (projects, droppedProjects) = Intersect(ProjectInput, Options.Projects);

        SelectedPersonIds = people;
        SelectedProjectIds = projects;

        var dropped = droppedPeople + droppedProjects;
        if (dropped > 0)
        {
            FilterNotice = dropped == 1
                ? "One item in that filter is no longer available, so the report was narrowed to the rest."
                : $"{dropped} items in that filter are no longer available, so the report was narrowed to the rest.";
        }
    }

    private static (HashSet<Guid> Resolved, int Dropped) Intersect(
        IReadOnlyCollection<string> requested, IReadOnlyList<FilterOption> offered)
    {
        if (requested.Count == 0)
        {
            return ([], 0);
        }

        var permitted = offered.Select(option => option.Id).ToHashSet();
        var resolved = requested
            .Select(value => Guid.TryParse(value, out var id) ? id : (Guid?)null)
            .Where(id => id is not null && permitted.Contains(id.Value))
            .Select(id => id!.Value)
            .ToHashSet();

        return (resolved, requested.Distinct().Count() - resolved.Count);
    }

    /// <summary>True when a filter was asked for and every one of its ids was dropped.</summary>
    private bool AskedForNothing() =>
        (ShowPersonFilter && PersonInput.Length > 0 && SelectedPersonIds.Count == 0)
        || (ProjectInput.Length > 0 && SelectedProjectIds.Count == 0);

    /// <summary>True when the address already spells out the range that was resolved.</summary>
    private bool MatchesAddress(DateRange range) =>
        TryParseDate(FromInput, out var begin) && TryParseDate(ToInput, out var end)
        && begin == range.Begin && end == range.End;

}
