# Feature Specification: Chart Drill-Through

<!--
  TEMPLATE INSTRUCTIONS
  =====================
  Fill in each section focusing on WHAT the feature does and WHY — not HOW it should
  be implemented.

  Usage:
  - One spec per feature or functional slice
  - Store in docs/specs/ and version-control alongside your code
  - Use [NEEDS CLARIFICATION: question] markers for unresolved decisions (max 3)
  - Remove optional sections that don't apply — don't leave them as N/A
  - Reference your arc42 architecture docs where relevant rather than duplicating them
-->

## 1. Overview

| Field           | Value                                                                    |
| --------------- | ------------------------------------------------------------------------ |
| Feature ID      | 014                                                                      |
| Status          | Draft                                                                    |
| Author          | gunther bogaert                                                          |
| Created         | 2026-09-15                                                               |
| Last updated    | 2026-09-15                                                               |
| Epic / Parent   | Story map activity 4 — *Report & visualize*                              |
| Arc42 reference | 05 Building Block View, 06 Runtime View, 08 Cross-cutting Concepts, 10 Quality Requirements, 12 Glossary |

> **Scope note.** This spec covers story **014** and depends entirely on
> [spec 012](012-date-range-project-report.md) (the report screen, the query, the
> scoping and the *slices reconcile* invariant) and [spec 013](013-report-pivot-dimensions.md)
> (the dimension and its slice keys). It adds one capability: turning a slice back into
> the entries that made it. It restates none of their rules.

> **Terminology.** **ReportQuery**, **ReportSlice**, **ReportResult** and *slices
> reconcile* come from spec 012. **ReportDimension**, **TimeBucket** and *one set, three
> partitions* come from spec 013. **TimeEntry** and **duration** come from
> [spec 001](001-daily-time-entry.md). **Person**, **Role** come from
> [spec 007](007-authentication-and-roles.md).

### 1.1 Problem Statement

A bar saying "Apollo: 168 hours" answers *how much* and refuses to answer *what*. The
moment a number looks wrong — too high, too low, unexpected — the only way to check it
today is to leave the report, guess which days and which people are involved, and read
timesheets one at a time. A number nobody can interrogate is a number nobody trusts,
and an untrusted report is not used, which returns the product to the flat report it
was built to replace.

### 1.2 Goal

Any figure on a report can be opened to reveal exactly the time entries that produced
it, listed and totalling to the figure that was clicked. Checking a suspicious number
costs one interaction and getting back to the report costs one more, so the report
becomes something a manager argues with rather than something they export.

### 1.3 Non-Goals

- Editing, deleting or creating entries from the drill-through. Correcting one's own
  entry is story 005 and correcting someone else's is story 011; this spec offers a
  **link** to those screens and no inline editing (FR-014)
- Drilling to an intermediate level (project → person → entries). One step, from slice
  to entries, by decision recorded in Section 9.2
- Any new filter, dimension, range control or chart — the drill-through inherits the
  report's query wholesale and adds only the slice
- Bulk actions, selection, or approval of the listed entries
- Export of the drilled list (story 016 exports the report; whether it also exports a
  drill-through is that story's question)
- A stored or recomputed audit trail of who drilled into what
- Charting the drilled entries; the drill-through is a list, because a list of
  individual entries is already at the finest grain the system has

## 2. User Stories

### US-001: Interrogate a number that looks wrong

**As a** manager,
**I want** to click a bar and see the entries behind it,
**so that** I can find out why a project consumed 40 more hours than I expected
without leaving the report.

### US-002: Find the entry to correct

**As a** manager,
**I want** the drilled list to take me to the entry I need to fix,
**so that** discovering a mistake and correcting it are the same journey.

### US-003: Check my own figure

**As a** developer,
**I want** to open a day or a project from my own report,
**so that** I can see what I actually logged there without reconstructing it from the
week view.

### US-004: Trust the drill

**As a** manager,
**I want** the drilled entries to total exactly the figure I clicked,
**so that** the detail is evidence for the summary rather than a second opinion about it.

### US-005: Get back to where I was

**As a** manager,
**I want** to return to the report with every filter, dimension and granularity intact,
**so that** checking one slice does not cost me the question I was asking.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                  | Priority | User Story |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall make every slice of a report openable — from the chart and from the corresponding table row alike.                                             | Must     | US-001     |
| FR-002 | The system shall, on opening a slice, list the individual time entries that produced it.                                                                       | Must     | US-001     |
| FR-003 | The system shall select those entries by the report's query (range, person filter, project filter, role scope) intersected with the slice's own key.             | Must     | US-004     |
| FR-004 | The system shall show, for each listed entry, its date, person, project, begin time, end time, duration in decimal hours, and note where story 003 has supplied one. | Must  | US-001     |
| FR-005 | The system shall show the total of the listed entries and that total shall equal the figure of the slice that was opened.                                       | Must     | US-004     |
| FR-006 | The system shall order the listed entries by date ascending, then begin time ascending, then person name ascending.                                             | Must     | US-001     |
| FR-007 | The system shall omit the column that is constant for the slice — project for a Project slice, person for a Person slice, date for a Day-granularity Time slice.  | Should   | US-001     |
| FR-008 | The system shall re-apply the viewer's role scope when the slice is opened, independently of the report that produced the link (spec 007, FR-010).               | Must     | US-004     |
| FR-009 | The system shall list, for an opened *Other* slice, the entries of every project or person collected into it, and shall name those projects or people.           | Should   | US-001     |
| FR-010 | The system shall list, for an opened *Time* slice, every entry dated within that bucket's begin and end dates inclusive.                                        | Must     | US-003     |
| FR-011 | The system shall present an opened empty *Time* bucket as an explicit empty list naming the bucket, not as an error or a dead control.                          | Should   | US-003     |
| FR-012 | The system shall carry the report query, the dimension, the granularity and the opened slice's key in the address, such that the drilled view is itself shareable. | Must   | US-005     |
| FR-013 | The system shall return the viewer to the report with its range, filters, dimension and granularity unchanged when the drilled view is closed.                   | Must     | US-005     |
| FR-014 | The system shall link each listed entry to the screen on which the viewer is permitted to correct it, and shall omit that link where the viewer has no such permission. | Should | US-002 |
| FR-015 | The system shall offer no control that creates, changes or deletes an entry within the drilled view itself.                                                     | Must     | US-002     |
| FR-016 | The system shall page the list at 200 entries, showing the slice's full total on every page alongside the count of entries in the slice.                        | Should   | US-001     |
| FR-017 | The system shall recompute the list at the moment the slice is opened, and shall state plainly when the recomputed total differs from the figure shown on the chart. | Should | US-004     |
| FR-018 | The system shall identify each listed person by full name, including deactivated people, with no indication of their password or account state beyond that name. | Must     | US-001     |
| FR-019 | The system shall make the drilled view reachable and operable without a pointing device.                                                                       | Should   | US-003     |

**Conventions applied where the story left details open:** "drilling into a slice of a
chart" is implemented as opening a slice, reachable identically from the bar and from
the table row, because requiring a click on a bar would make the feature unusable by
keyboard and by screen reader; the drilled list is always the *entries*, never an
intermediate grouping; the slice key travels in the address rather than the slice's
label, so renaming a project never breaks a shared drill link.

## 4. Acceptance Scenarios

### SC-001: Opening a project slice (FR-002, FR-003, FR-005)

```gherkin
Given Mo is reporting on September 2026 by Project
  And "Apollo" shows 168.00 hours
When he opens the "Apollo" slice
Then the entries listed are exactly the September 2026 entries on "Apollo"
  And their total reads 168.00
```

### SC-002: Opening from the table row (FR-001)

```gherkin
Given Mo is reporting on September 2026 by Project
When he opens the "Apollo" row of the table instead of the bar
Then he reaches the same drilled list as SC-001
```

### SC-003: The filter follows the drill (FR-003)

```gherkin
Given Mo is reporting on September 2026 by Project, filtered to the person Ann
  And "Apollo" shows 100.00 hours
When he opens the "Apollo" slice
Then only Ann's entries are listed
  And their total reads 100.00
```

### SC-004: Opening a person slice (FR-003, FR-007)

```gherkin
Given Mo is reporting on September 2026 by Person
  And "Ann" shows 160.00 hours
When he opens the "Ann" slice
Then every listed entry belongs to Ann
  And no person column is shown
  And the total reads 160.00
```

### SC-005: Opening a day (FR-010)

```gherkin
Given Ann is reporting on September 2026 by Time at Day granularity
  And 2026-09-15 shows 7.50 hours
When she opens that bucket
Then her entries dated 2026-09-15 are listed in begin-time order
  And the total reads 7.50
```

### SC-006: Opening a week bucket (FR-010)

```gherkin
Given Mo is reporting on 2026-04-01 to 2026-09-30 by Time at Week granularity
  And the bucket 2026-09-14 to 2026-09-20 shows 210.00 hours
When he opens it
Then every listed entry is dated between 2026-09-14 and 2026-09-20 inclusive
  And the total reads 210.00
```

### SC-007: Opening an empty bucket (FR-011)

```gherkin
Given Ann's report by Time shows 0.00 for 2026-09-15
When she opens that bucket
Then she is told no hours are logged for 2026-09-15
  And no entries are listed
```

### SC-008: Opening the Other slice (FR-009)

```gherkin
Given Mo's report by Project shows "Other (5 projects)" at 30.00 hours
When he opens it
Then the entries of all five projects are listed
  And those five projects are named
  And the total reads 30.00
```

### SC-009: A User drills into their own figure (FR-008)

```gherkin
Given Ann has the User role
  And her report by Project shows "Apollo" at 12.00 hours
When she opens the "Apollo" slice
Then only her own entries are listed
  And the total reads 12.00
```

### SC-010: A shared drill link is rescoped (FR-008, FR-012)

```gherkin
Given Mo shares the address of the "Apollo" drill showing 168.00 hours
When Ann, who has the User role, opens it
Then only Ann's entries on "Apollo" in that range are listed
  And the total reads 12.00, not 168.00
```

### SC-011: Returning to the report (FR-013)

```gherkin
Given Mo is reporting on 2026-04-01 to 2026-09-30 by Time at Week granularity,
      filtered to "Apollo"
When he opens a bucket and then closes the drilled view
Then the report shows the same range, dimension, granularity and filter
```

### SC-012: From a wrong entry to its correction (FR-014)

```gherkin
Given Mo has the Manager role
  And a drilled list contains Ann's 09:00–19:00 entry on "Apollo"
When he follows that entry's correction link
Then he reaches the screen on which he may amend Ann's entry (story 011)
```

### SC-013: No correction link without the permission (FR-014)

```gherkin
Given Ann has the User role
  And a drilled list contains her own entry and no other person's
When she views the list
Then her own entry links to the screen on which she may amend it (story 005)
```

### SC-014: The drilled view writes nothing (FR-015)

```gherkin
Given Mo has opened a slice containing Ann's entries
When he inspects the drilled view
Then no control within it edits, deletes or creates an entry
```

### SC-015: A large slice is paged (FR-016)

```gherkin
Given an opened slice contains 640 entries totalling 2,400.00 hours
When the drilled view is shown
Then the first 200 entries are listed
  And the view states 640 entries totalling 2,400.00 hours
  And the total is unchanged on every page
```

### SC-016: The slice moved while the report was on screen (FR-017)

```gherkin
Given Mo's report shows "Apollo" at 168.00 hours
  And Ann adds a 4.00-hour entry to "Apollo" in that range before he opens the slice
When he opens the "Apollo" slice
Then 172.00 hours of entries are listed
  And he is told the figure has changed since the report was computed
```

### SC-017: Ordering within a slice (FR-006)

```gherkin
Given an opened slice contains Bob's 2026-09-15 09:00 entry, Ann's 2026-09-15 09:00
      entry, and Ann's 2026-09-14 16:00 entry
When the list is shown
Then the order is Ann 2026-09-14 16:00, then Ann 2026-09-15 09:00, then Bob 2026-09-15 09:00
```

### SC-018: Drilling without a mouse (FR-019)

```gherkin
Given Ann is on the report screen using only the keyboard
When she moves to a slice and activates it
Then the drilled list opens and receives focus
  And closing it returns focus to the slice she opened
```

## 5. Domain Model

This feature introduces **no persisted entity** and adds no attribute to `TimeEntry`.
It introduces one value object — the coordinate of a slice — and reads entries that
already exist.

### 5.1 Entities

None. Spec 012's *nothing is stored* invariant holds unchanged, and this spec adds a
second: *drilling changes nothing*.

#### TimeEntry (referenced, not owned)

Defined by [spec 001](001-daily-time-entry.md). This is the only thing the drilled view
displays. It reads `date`, `personId`, `projectId`, `beginTime`, `endTime`, the derived
`duration`, and `note` once story 003 adds it. It never writes.

#### Person, Project (referenced, not owned)

Read for labels only: `Person.fullName` (FR-018) and `Project.name`.

### 5.2 Relationships

- A **ReportSlice** (spec 012/013) is backed by exactly the set of **TimeEntry** records
  that the report's **ReportQuery** selects and whose dimension value matches the
  slice's key. This spec's whole job is to show that set.
- A **DrillQuery** is a ReportQuery plus a **SliceKey**; it selects a subset of what the
  ReportQuery selects, never anything outside it.
- Every **TimeEntry** in a report belongs to exactly one drillable slice per dimension
  (spec 013, *one set, three partitions*). Consequently, drilling every slice of a
  dimension and concatenating the results reproduces the report's full selection with no
  duplicates and no omissions.

### 5.3 Value Objects

#### SliceKey

The coordinate of one slice, sufficient on its own — given the query — to reproduce the
slice's entry set.

| Attribute | Type                    | Constraints                                                                              |
| --------- | ----------------------- | ------------------------------------------------------------------------------------------ |
| dimension | ReportDimension         | required; must match the dimension of the query it accompanies                              |
| value     | UUID, date, or *Other*  | required; a project id, a person id, the begin date of a TimeBucket, or the *Other* marker   |

Keyed by identity, never by label (spec 013), so a project renamed after a link was
shared still drills correctly.

#### DrillQuery

| Attribute | Type            | Constraints                                                        |
| --------- | --------------- | ------------------------------------------------------------------ |
| query     | ReportQuery     | required; inherited from the report unchanged                       |
| dimension | ReportDimension | required                                                            |
| slice     | SliceKey        | required; its dimension equals `dimension`                          |
| page      | integer         | required, >= 1, default 1                                           |

This is exactly what FR-012 carries in the address. Like a ReportQuery, it is **not** a
permission: the viewer's scope is applied on top of it on every request (FR-008).

#### DrillResult

| Attribute    | Type               | Constraints                                                                 |
| ------------ | ------------------ | --------------------------------------------------------------------------- |
| entries      | list of TimeEntry  | ordered per FR-006; at most one page of 200                                  |
| entryCount   | integer            | required, >= 0; the count for the whole slice, not the page                  |
| totalMinutes | integer            | required, >= 0; the total for the whole slice, not the page                  |

### 5.4 Domain Rules and Invariants

- **The drill reconciles with its slice**: the drilled total equals the slice's figure
  in the report it was opened from, computed over the same data. This is the feature's
  entire reason to exist; a drill that does not reconcile is worse than no drill,
  because it discredits a report that may be correct.
- **A drill narrows, never widens**: the drilled set is a subset of the report's
  selected set. No entry can be reached by drilling that the report did not already
  count — this is what makes a drill safe to expose from a chart.
- **Scope is re-derived, never inherited from the link**: the viewer's role and the
  permitted person and project sets are resolved on the drill request itself. A shared
  drill address is a question, not a grant (spec 007, *server-side enforcement*).
- **The key is identity**: slices are addressed by project id, person id or bucket begin
  date. Labels are never part of the address, so renames and re-labelling cannot
  misdirect a drill.
- **Drilling changes nothing**: no path through this feature writes. The correction link
  (FR-014) leaves the feature entirely and lands on story 005 or 011, where that
  screen's own rules — including spec 001's overlap rule — apply in full.
- **Every slice is drillable, including the empty one**: an empty *Time* bucket opens to
  an empty list (FR-011). A control that exists but does nothing teaches people not to
  trust controls.
- **The partition is exhaustive**: drilling all slices of a dimension accounts for every
  entry the report counted, exactly once. Any entry that no slice can reach is an entry
  the report silently dropped.
- **Paging does not change the total**: `totalMinutes` and `entryCount` describe the
  slice, not the page. A total that shrank on page two would recreate the very doubt
  this feature removes.
- **Recency over consistency**: the drilled list is recomputed at the moment it is
  opened, so it can legitimately differ from the chart (FR-017, spec 012, EC-11). The
  difference is disclosed rather than hidden, and the live figure is never adjusted to
  match a stale chart.

## 6. Non-Functional Requirements

| ID      | Category      | Requirement                                                                                                                                                  |
| ------- | ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance   | A slice opens in < 800 ms at p95 for a slice of up to 200 entries, so interrogating a figure feels like part of the report rather than a second screen.           |
| NFR-002 | Performance   | Paging within an opened slice completes in < 800 ms at p95 at any page depth, including a slice of 100 000 entries.                                              |
| NFR-003 | Scalability   | A slice of arbitrary size never loads more than one page of entries into a response; the count and total are computed in the store, not by materialising the set. |
| NFR-004 | Correctness   | The drilled total equals the slice figure to two decimal places for every slice of every dimension, with no accumulated rounding error (spec 012, NFR-004).       |
| NFR-005 | Security      | The drill query applies the viewer's scope in the store query, so no entry the viewer may not see is ever loaded (spec 012, NFR-005).                            |
| NFR-006 | Security      | The drilled view exposes no attribute of a Person beyond their full name — never an email address, role or account state.                                        |
| NFR-007 | Accessibility | Every slice is reachable, activatable and labelled for a screen reader from the table as well as the chart; opening moves focus into the list and closing returns it to the origin slice. |
| NFR-008 | Usability     | Opening a slice and returning is at most one interaction each, and returning never re-asks the report's question (FR-013).                                       |

Project-wide quality requirements belong in arc42 Section 10 and are referenced here,
not restated.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                       | Expected Behavior                                                                                                                       |
| ----- | ---------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A slice key in the address names a project or person that does not exist                        | An empty drilled list stating that the slice holds no hours; existence is never confirmed or denied by a distinct error (spec 012, EC-5)   |
| EC-2  | A slice key names a project the viewer may not see                                             | Treated identically to EC-1 — empty, with no disclosure that the project exists                                                          |
| EC-3  | A slice key whose dimension disagrees with the query's dimension, e.g. a person id under *Time*  | The whole drill is refused and the report is shown instead, with a message. A mismatched key is never coerced into a valid one            |
| EC-4  | Every entry in the slice is deleted (story 005) between the report and the drill                | An empty list with FR-017's changed-since-computed notice; not an error                                                                   |
| EC-5  | An entry is moved to another project between the report and the drill                          | It is absent from the old project's drill and present in the new one's; FR-017's notice explains the difference from the chart            |
| EC-6  | The *Other* slice is opened when the report's data has shifted so that its membership changed   | Membership is recomputed at drill time from the current top-15 rule and the projects it now covers are named. FR-017's notice applies      |
| EC-7  | A *Time* bucket is opened whose range extends outside the report range (a partial edge bucket)  | Only entries inside the report range are listed, because the bucket is clipped to the range (spec 013, buckets tile the range)            |
| EC-8  | A slice contains entries from deactivated people                                                | Listed normally by full name (FR-018); their state is not shown, because it is irrelevant to the hours and would disclose HR information   |
| EC-9  | A slice contains entries on archived projects                                                   | Listed normally with the project name; archiving affects booking, never reporting (spec 012, FR-017)                                      |
| EC-10 | A page number beyond the last page arrives in the address                                       | The last page is shown, with the count and total unchanged; not an error                                                                  |
| EC-11 | The report's slice figure and the drilled total differ                                          | Both are shown, the drilled total is the authoritative one, and FR-017's notice says the report was computed earlier. The chart is never silently rewritten |
| EC-12 | A slice of exactly 200 entries, and one of 201                                                  | 200 is one page with no pager; 201 is two pages. The count and total are stated in both cases                                             |
| EC-13 | The viewer's role changes between opening the report and opening a slice                        | The drill is scoped by the new role (FR-008, spec 007, FR-017); the drilled total may legitimately be smaller than the chart's figure      |
| EC-14 | The session expires while a drilled view is open                                                | Spec 007, EC-12 applies: redirected to sign-in, returned to the same drill address afterwards (FR-012)                                    |
| EC-15 | An entry carries a very long note (story 003)                                                   | Truncated for layout with the full note obtainable; the note is never altered in storage (spec 001, EC-16 applies the same rule to names)  |
| EC-16 | A slice is opened from a report whose range was invalid and was refused                         | Unreachable by construction — there is no result and therefore no slice. If such an address is constructed by hand, spec 012, EC-4 applies |
| EC-17 | Two entries in a slice share date, begin time and person (impossible per spec 001's overlap rule) | Cannot occur. If it ever does, spec 001's *no hour is booked twice* invariant has been violated and the drill is the screen that reveals it |
| EC-18 | A Person-dimension slice is drilled by a viewer whose role has since dropped to User            | Only their own entries are listed, so the drilled total will not reconcile with the chart; EC-13's behaviour and FR-017's notice apply     |

## 8. Success Criteria

<!-- Success criteria are prefixed SUC- to avoid colliding with the SC- acceptance
     scenario identifiers in Section 4. -->

| ID      | Criterion                                                                                                                                 |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All acceptance scenarios SC-001 to SC-018 pass automatically in CI                                                                          |
| SUC-002 | A property-based test confirms that for every dimension and every slice of a generated report, the drilled total equals the slice figure     |
| SUC-003 | A property-based test confirms that drilling every slice of a dimension yields each selected entry exactly once — no duplicates, no omissions |
| SUC-004 | Replaying every drill address as each of the three roles discloses no entry the role may not see, verified against a direct query            |
| SUC-005 | A manager goes from a suspicious figure to the entry that explains it in two interactions, and back to the unchanged report in one           |
| SUC-006 | The drilled view issues no write of any kind, verified by asserting an unchanged store across the full scenario suite                        |
| SUC-007 | NFR-002 is met against a seeded slice of 100 000 entries at the last page                                                                   |
| SUC-008 | Every slice is reachable and activatable by keyboard alone, verified across all three dimensions                                             |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 012 (date-range project report)** — supplies the query, the scoping, the
  *slices reconcile* invariant and the addressable-query convention this spec extends.
- **Spec 013 (report pivot dimensions)** — supplies the dimension, the slice keys and
  the *Time* buckets. Every dimension 013 adds must be drillable on the day it lands,
  including any future one (see Open Question 1).
- **Spec 001 (daily time entry)** — supplies every field the drilled list displays.
- **Spec 007 (authentication and roles)** — supplies the per-request scoping of FR-008
  and the permission that decides whether FR-014's correction link is shown.
- **Story 003 (notes)** — FR-004's note column is empty until it lands; nothing here
  blocks on it.
- **Story 005 (edit/delete own entry)** and **story 011 (correct another person's
  entry)** — the targets of FR-014. Until they exist, FR-014 is omitted and the drilled
  view is purely a list; the rest of the feature is unaffected.
- **Story 015 (burn-down)** reuses this drill for its consumed-hours figure. That link is
  specified in [spec 015](015-project-burn-down.md), FR-018, and is constrained by a
  scoping subtlety recorded there: a User may see a project's *aggregate* consumption
  without being permitted to drill it.
- **Story 016 (export)** — whether a drilled list is exportable is that story's decision,
  not this one's.

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10; root namespace `pmsa`.
- **Accepted tension:** the drilled total may differ from the figure that was clicked
  (FR-017, EC-11), because the report is computed on demand and is not live (spec 012,
  Section 9.2). Freezing the report's data to guarantee agreement would make every
  report a snapshot with its own staleness problem; disclosing the difference is the
  lesser evil. Recorded in arc42 Section 11.
- **Accepted tension:** one drill step only (Section 1.3). A manager who wants
  "Apollo, then per person, then the entries" must instead filter to Apollo and pivot to
  Person (spec 013) — two interactions rather than a nested expansion. Filter-and-pivot
  reuses an existing screen with an existing invariant, where nesting would need a new
  partition model and new reconciliation rules at every level.
- **Accepted tension:** the correction link (FR-014) leads out of the report into
  story 005/011, so the report loses its place on the way there and NFR-008's promise
  ends at that boundary. Editing inline would put spec 001's overlap refusal inside a
  reporting screen, which is a worse trade.

### 9.3 Architecture References

All arc42 sections are currently empty templates. The table states what this spec
expects each to say once written.

| Arc42 Section                | Relevance to This Feature                                                                                                      |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| 05. Building Block View      | Adds the drill path to the reporting building block; it reads the same `TimeEntry` store as the report, at entry grain rather than aggregate grain |
| 06. Runtime View             | The drill sequence: resolve scope, intersect the report query with the slice key, count and total in the store, fetch one page      |
| 08. Cross-cutting Concepts   | The reconcile-with-its-slice rule and the addressable-query convention; both apply to every future aggregate screen                 |
| 09. Architecture Decisions   | Needs ADRs for: one drill step rather than nested drill-down; recomputing at drill time and disclosing divergence rather than snapshotting |
| 10. Quality Requirements     | NFR-004 (aggregate/detail agreement) is a project-wide correctness scenario worth promoting                                        |
| 11. Risks & Technical Debt   | Chart/drill divergence under concurrent edits; loss of report context when following a correction link                             |
| 12. Glossary                 | Drill-through, slice, slice key, coordinate, reconcile, page, Other slice                                                          |

## 10. Open Questions

| #   | Question                                                                                                | Owner        | Status   | Resolution                                                                                                                  |
| --- | --------------------------------------------------------------------------------------------------------- | ------------ | -------- | ----------------------------------------------------------------------------------------------------------------------------- |
| 1   | If a *Client* dimension is added to spec 013, does it drill straight to entries or to projects first?     | Product      | Open     | Not a blocker. The default answer is straight to entries, consistent with the one-step rule in Section 9.2                    |
| 2   | Is 200 entries the right page size, given that a month of a 10-person team is roughly 400 entries?        | Product      | Open     | Revisit after first use; changing it affects FR-016 and EC-12 only, and no invariant                                          |
| 3   | Should a drilled list be exportable, or is exporting only ever the report (story 016)?                    | Product      | Deferred | Story 016's decision. This spec neither provides nor forecloses it                                                            |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/002/008/015/017,
        US-002 SC-012/013/014, US-003 SC-005/006/007/018, US-004 SC-003/009/010/016,
        US-005 SC-011)
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections (sections are empty templates;
        the table states what each must contain)
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero present)
  - [x] Open questions are assigned and have a resolution path
-->
