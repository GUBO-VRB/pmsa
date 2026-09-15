# Feature Specification: Date-Range Project Report

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
| Feature ID      | 012                                                                      |
| Status          | Draft                                                                    |
| Author          | gunther bogaert                                                          |
| Created         | 2026-09-15                                                               |
| Last updated    | 2026-09-15                                                               |
| Epic / Parent   | Story map activity 4 — *Report & visualize*                              |
| Arc42 reference | 05 Building Block View, 06 Runtime View, 08 Cross-cutting Concepts, 10 Quality Requirements, 12 Glossary |

> **Scope note.** This spec covers story **012** only, but it owns the *report screen*
> that stories 013, 014 and 016 extend. Everything those three specs need in common —
> the filter, the scoping rules, the aggregation and rounding rules, and the
> chart-plus-table pairing — is defined here once. [Spec 013](013-report-pivot-dimensions.md)
> adds the grouping dimension, [spec 014](014-chart-drill-through.md) adds drill-through,
> story 016 adds export. [Spec 015](015-project-burn-down.md) is a *different* screen
> with deliberately different semantics (see its Section 1.3).

> **Terminology.** **TimeEntry**, **duration**, **decimal hours** and the
> *totals are derived* invariant come from [spec 001](001-daily-time-entry.md).
> **Person**, **Role** and the User/Manager/Admin capability matrix come from
> [spec 007](007-authentication-and-roles.md). **Project** is owned by story 008; this
> spec consumes its `name`, `client` and `isActive`.

### 1.1 Problem Statement

The tool being replaced produces a single flat report, so the hours people spend
logging are returned to nobody as insight — a manager who wants to know where last
month actually went exports rows and rebuilds the answer in a spreadsheet, and a
developer who wants to know the same thing has no answer at all. Hours that can only
be read one row at a time are a cost with no payback, which is exactly why logging
them feels like overhead.

### 1.2 Goal

Anyone signed in can pick a span of time and immediately see how those hours divided
across projects — as a chart they can read in a glance and a table they can read
precisely — with the numbers scoped to what their role permits them to see. The
report is a live query over time entries, not a generated artefact, so it is never
stale and it is shareable as a link.

### 1.3 Non-Goals

- Pivoting the same data per person or per day (story 013) — this spec groups by
  project only, but Section 5 is shaped so that 013 adds a dimension rather than a
  second report
- Drilling from a chart slice into the underlying entries (story 014)
- Project budgets, consumption percentages and burn-down (stories 010, 015)
- CSV/Excel export (story 016)
- **Money of any kind.** The system holds no hourly rates, no cost and no revenue —
  nothing in the story map introduces them. Reports are denominated in hours, full stop
- Saved, named report definitions — the address of the report *is* the saved report
  (FR-016)
- Scheduled or emailed reports — no email infrastructure exists (spec 007, Section 9.2)
- Cross-tabulation (project × person in one table) — one grouping at a time, by
  decision recorded in spec 013
- Editing anything from the report; it is strictly read-only (FR-020)
- Comparison against a previous period, forecasts, or trend lines

## 2. User Stories

### US-001: See where a span of time went

**As a** developer,
**I want** to pick a date range and see my hours broken down per project,
**so that** logging time gives me something back instead of only costing me something.

### US-002: Answer a question about the month without a spreadsheet

**As a** manager,
**I want** the same breakdown across everybody's hours,
**so that** I can answer "where did September go on Apollo?" on the screen rather
than by exporting rows.

### US-003: Narrow the question

**As a** manager,
**I want** to restrict the report to particular projects or particular people,
**so that** one client conversation is not buried under every other project.

### US-004: Read the numbers, not just the picture

**As a** manager,
**I want** the exact hours and share alongside the chart,
**so that** I can quote a figure rather than estimate one from a bar.

### US-005: Send someone the report

**As a** manager,
**I want** to share the report I am looking at as a link,
**so that** a colleague opens the same question without me describing the filters.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                 | Priority | User Story |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall provide a report screen available to every signed-in person, regardless of role.                                                              | Must     | US-001     |
| FR-002 | The system shall report over a date range given as a begin date and an end date, both inclusive.                                                               | Must     | US-001     |
| FR-003 | The system shall offer the presets *This week*, *Last week*, *This month*, *Last month*, *This quarter*, *This year* and *Custom*, defaulting to *This month*.  | Must     | US-001     |
| FR-004 | The system shall treat a week as starting on Monday (ISO-8601) wherever a week is a preset or a bucket.                                                        | Must     | US-001     |
| FR-005 | The system shall reject a range whose end date precedes its begin date, leaving the previous result on screen.                                                 | Must     | US-001     |
| FR-006 | The system shall reject a range longer than 366 days.                                                                                                        | Should   | US-001     |
| FR-007 | The system shall group the time entries in range by project and sum their durations.                                                                          | Must     | US-001     |
| FR-008 | The system shall present the grouped result as a horizontal bar chart ordered by hours descending, ties broken by project name ascending.                      | Must     | US-001     |
| FR-009 | The system shall present, alongside the chart, a table giving each project's name, client, hours in decimal to two places, and share of the total to one place.| Must     | US-004     |
| FR-010 | The system shall show the grand total of the reported hours in decimal to two places.                                                                        | Must     | US-004     |
| FR-011 | The system shall omit from the result any project with no hours in the range.                                                                                 | Must     | US-001     |
| FR-012 | The system shall chart at most the 15 highest projects and collect the remainder into a single *Other (N projects)* slice, while the table lists every project.| Should   | US-004     |
| FR-013 | The system shall restrict a person with the User role to their own time entries (spec 007, FR-012).                                                            | Must     | US-001     |
| FR-014 | The system shall include every person's entries for the Manager and Admin roles (spec 007, FR-011).                                                            | Must     | US-002     |
| FR-015 | The system shall offer a multi-select person filter to the Manager and Admin roles only, and omit the control entirely for the User role (spec 007, FR-009).    | Should   | US-003     |
| FR-016 | The system shall offer a multi-select project filter listing only projects the person is permitted to see.                                                     | Should   | US-003     |
| FR-017 | The system shall include the entries of deactivated people and of archived projects whenever they fall in range (spec 007, FR-016).                             | Must     | US-002     |
| FR-018 | The system shall carry the complete filter state — range, person filter, project filter — in the address of the report, such that opening that address reproduces the same question. | Must | US-005 |
| FR-019 | The system shall apply the opening person's own scoping to a shared address, never the sender's (FR-013).                                                      | Must     | US-005     |
| FR-020 | The system shall present a report with no hours as an explicit empty result naming the range, not as an empty chart.                                            | Should   | US-001     |
| FR-021 | The system shall compute every figure from the time entries at request time and store no aggregate.                                                            | Must     | US-001     |
| FR-022 | The system shall offer no action that creates, changes or deletes a time entry from the report screen.                                                        | Must     | US-002     |
| FR-023 | The system shall compute shares from whole minutes and present them to one decimal place, and shall state the rounding when the presented shares do not total exactly 100.0. | Should | US-004 |

**Conventions applied where the story left details open:** ranges are inclusive on both
ends and are compared against a TimeEntry's `date`, never its `createdAt`; a range may
extend into the future but contributes nothing, because future entries cannot exist
(spec 001, FR-011); hours are decimal to two places and are always summed as minutes
and converted once (spec 001, `Duration`); an unfiltered report means *everything the
role permits*, not *everything*.

## 4. Acceptance Scenarios

### SC-001: Hours per project for a month (FR-002, FR-007, FR-009)

```gherkin
Given Ann logged 12:00 hours on "Apollo" and 8:00 hours on "Atlas" in September 2026
When she reports on 2026-09-01 to 2026-09-30
Then the result lists "Apollo" 12.00 and "Atlas" 8.00
  And the grand total reads 20.00
```

### SC-002: The default range (FR-003)

```gherkin
Given the current date is 2026-09-15
When Ann opens the report screen
Then the range is 2026-09-01 to 2026-09-30
  And the preset "This month" is selected
```

### SC-003: A week starts on Monday (FR-004)

```gherkin
Given the current date is Tuesday 2026-09-15
When Ann selects the preset "This week"
Then the range is 2026-09-14 to 2026-09-20
```

### SC-004: Ordering and ties (FR-008)

```gherkin
Given "Zeus" and "Apollo" each hold 6.00 hours and "Atlas" holds 9.00 hours in range
When the report is shown
Then the order is "Atlas", "Apollo", "Zeus"
```

### SC-005: A backwards range is refused (FR-005)

```gherkin
Given Ann is viewing a report for September 2026
When she sets the begin date to 2026-09-30 and the end date to 2026-09-01
Then no new result is computed
  And she is told the end date must not precede the begin date
  And the September result is still on screen
```

### SC-006: An over-long range is refused (FR-006)

```gherkin
When Ann requests 2024-01-01 to 2026-09-15
Then no result is computed
  And she is told a report covers at most 366 days
```

### SC-007: Projects with no hours are absent (FR-011)

```gherkin
Given the active project "Hermes" exists and has no entries in September 2026
When the report for September 2026 is shown
Then "Hermes" appears neither in the chart nor in the table
```

### SC-008: A User sees only their own hours (FR-013)

```gherkin
Given Ann has the User role
  And Ann logged 12.00 hours and Bob logged 30.00 hours on "Apollo" in September 2026
When Ann reports on September 2026
Then "Apollo" reads 12.00
  And no figure on the screen includes Bob's hours
```

### SC-009: A Manager sees everybody (FR-014)

```gherkin
Given Mo has the Manager role
  And Ann logged 12.00 hours and Bob logged 30.00 hours on "Apollo" in September 2026
When Mo reports on September 2026
Then "Apollo" reads 42.00
```

### SC-010: The person filter is absent for a User (FR-015)

```gherkin
Given Ann has the User role
When she opens the report screen
Then no person filter is offered
```

### SC-011: Filtering to one project (FR-016)

```gherkin
Given Mo reports on September 2026 across "Apollo", "Atlas" and "Zeus"
When he restricts the project filter to "Apollo"
Then only "Apollo" is charted
  And the grand total covers "Apollo" alone
```

### SC-012: A leaver's hours still count (FR-017)

```gherkin
Given Bob logged 30.00 hours on "Apollo" in September 2026
  And an Admin has since deactivated Bob
When Mo reports on September 2026
Then "Apollo" still reads 30.00 including Bob's hours
```

### SC-013: An archived project still reports (FR-017)

```gherkin
Given "Apollo" holds 42.00 hours in September 2026 and has since been archived
When Mo reports on September 2026
Then "Apollo" appears with 42.00 hours
```

### SC-014: The report is a link (FR-018)

```gherkin
Given Mo is reporting on 2026-09-01 to 2026-09-30 filtered to "Apollo"
When he copies the address and Dee opens it
Then Dee sees the same range and the same project filter already applied
```

### SC-015: A shared link is rescoped to the opener (FR-019)

```gherkin
Given Mo has the Manager role and shares a report showing 42.00 hours on "Apollo"
When Ann, who has the User role, opens that same address
Then she sees the same range and filter
  And "Apollo" reads only her own 12.00 hours
```

### SC-016: A range with nothing in it (FR-020)

```gherkin
Given Ann logged no hours between 2026-07-01 and 2026-07-31
When she reports on that range
Then no chart is drawn
  And she is told there are no hours logged between 2026-07-01 and 2026-07-31
```

### SC-017: The long tail is collected (FR-012)

```gherkin
Given 20 projects hold hours in the range
When the report is shown
Then the chart holds 15 project bars and one bar labelled "Other (5 projects)"
  And the "Other" bar equals the sum of those 5 projects
  And the table lists all 20 projects individually
```

### SC-018: Shares add up (FR-023)

```gherkin
Given the range holds 12.00 hours on "Apollo" and 8.00 hours on "Atlas"
When the report is shown
Then "Apollo" shows 60.0% and "Atlas" shows 40.0%
  And the shares total 100.0%
```

### SC-019: The report changes nothing (FR-022)

```gherkin
Given Mo is viewing a report covering Ann's entries
When he inspects the screen
Then no control edits, deletes or creates an entry
```

## 5. Domain Model

This feature introduces **no persisted entity**. Every figure it shows is derived from
`TimeEntry` (spec 001), `Person` (spec 007) and `Project` (story 008) at request time.
The objects below are the vocabulary of a *query and its answer*; they exist for the
duration of a request and are named so that specs 013, 014 and 016 can extend them
rather than invent parallel ones.

### 5.1 Entities

None. See the *Nothing is stored* invariant in Section 5.4.

#### TimeEntry (referenced, not owned)

Defined by [spec 001](001-daily-time-entry.md). This spec reads `personId`,
`projectId`, `date`, and the derived `duration`. It never writes.

#### Project (referenced, not owned)

Defined by story 008. This spec reads `id`, `name`, `client` and `isActive`. `client`
is displayed as a column (FR-009) and is a label only — it is not a grouping dimension
in this spec and confers no access boundary (spec 007, Section 1.3).

#### Person (referenced, not owned)

Defined by [spec 007](007-authentication-and-roles.md). This spec reads `id`,
`fullName`, `role` and `isActive`.

### 5.2 Relationships

- A **ReportQuery** selects a set of **TimeEntry** records; that set is partitioned into
  **ReportSlice**s, one per project, and the slices plus the grand total make a
  **ReportResult**.
- Every **TimeEntry** in the selected set belongs to exactly one slice. The partition is
  total and disjoint, which is what makes the *slices reconcile* invariant testable.
- A **ReportSlice** names a **Project**; the *Other* slice (FR-012) names several and is
  a presentation grouping applied after the partition, never a different partition.

### 5.3 Value Objects

#### DateRange

| Attribute | Type | Constraints                                               |
| --------- | ---- | --------------------------------------------------------- |
| begin     | date | required                                                   |
| end       | date | required, not before `begin`, at most 365 days after it     |

Inclusive on both ends: `2026-09-01`–`2026-09-01` is one day, not zero.

#### ReportQuery

The whole question, and exactly what FR-018 carries in the address.

| Attribute  | Type            | Constraints                                                              |
| ---------- | --------------- | ------------------------------------------------------------------------ |
| range      | DateRange       | required                                                                  |
| personIds  | set of UUID     | optional; empty means *all the role permits*; ignored for the User role    |
| projectIds | set of UUID     | optional; empty means *all the role permits*                              |

A ReportQuery is **not** by itself a permission. The viewer's role is applied on top of
it on every request (FR-019), which is why the query is safe to put in a shareable
address.

#### ReportSlice

| Attribute | Type    | Constraints                                          |
| --------- | ------- | ---------------------------------------------------- |
| key       | UUID    | required; the project's id (or the *Other* marker)    |
| label     | string  | required; the project's name                          |
| minutes   | integer | required, > 0 — a zero slice is omitted (FR-011)      |

`hours` and `share` are derived from `minutes`, never stored: `hours = minutes / 60`
to two decimal places, `share = minutes / totalMinutes` to one decimal place.

#### ReportResult

| Attribute    | Type               | Constraints                                           |
| ------------ | ------------------ | ----------------------------------------------------- |
| query        | ReportQuery        | required; the question this answers                    |
| slices       | list of ReportSlice| ordered by minutes descending, then label ascending     |
| totalMinutes | integer            | required, >= 0, equals the sum of the slices' minutes   |

### 5.4 Domain Rules and Invariants

- **Nothing is stored**: no aggregate, roll-up or cached total survives the request.
  This is spec 001's *totals are derived* invariant carried into reporting; it is what
  makes a report incapable of disagreeing with the entries beneath it.
- **The slices reconcile**: the sum of the slices' minutes equals `totalMinutes`
  exactly, and every selected entry appears in exactly one slice. A report that drops or
  double-counts an entry is wrong even if every displayed number looks plausible.
- **Rounding happens once, at the end**: minutes are summed as integers and converted to
  decimal hours only for presentation. Totals are never computed by adding rounded
  hours — otherwise the total disagrees with its own rows.
- **Membership is by date, not by when it was typed**: an entry belongs to the range if
  its `date` falls inside it. Back-dated entries typed today appear in the month they
  were worked, and a report re-run later will legitimately show a different number for a
  range already reported. Reports are current, not immutable.
- **Scoping is applied server-side on every request**: the role decides what the query
  may see (FR-013, FR-014). Nothing in the address, the filter or a shared link can
  widen it — this is spec 007's *server-side enforcement* invariant.
- **A filter narrows, never widens**: `personIds` and `projectIds` intersect with what
  the role permits. A User's person filter, were one ever supplied in the address, can
  only ever resolve to themselves.
- **Deactivation and archiving are not deletion**: the hours of deactivated people and
  archived projects stay in every report forever (spec 007, FR-016). A report that moved
  when someone left would make historical figures unreproducible.
- **Zero is absent, not zero**: a project with no hours in the range is not a slice with
  0.00 — it is not in the result at all. (Spec 013 deliberately breaks this rule for the
  Day dimension, where an empty day is the point.)
- **The report is read-only**: no path through this feature writes anything.
- **Hours only**: no figure in any report is money. There is no rate anywhere in the
  system to convert with.

## 6. Non-Functional Requirements

| ID      | Category      | Requirement                                                                                                                                             |
| ------- | ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance   | A report over a 31-day range across 200 people and 500 projects renders in < 1 s at p95; over the maximum 366-day range, in < 2.5 s at p95.                 |
| NFR-002 | Performance   | Changing a filter re-renders the result in < 1 s at p95 without a full page reload.                                                                        |
| NFR-003 | Scalability   | NFR-001 holds at the data volume of spec 001, NFR-006 (~1.5 M entries over 3 years), which requires the store to resolve (date-range, person, project) without scanning the whole table. |
| NFR-004 | Correctness   | The grand total equals the sum of the displayed slice hours to the displayed precision, for every range, with no accumulated rounding error.                |
| NFR-005 | Security      | Scoping (FR-013) is enforced in the query that fetches the entries, not by filtering after retrieval, so that no unpermitted hour is ever loaded into a response. |
| NFR-006 | Accessibility | The chart is not the only way to read the result: the table of FR-009 conveys every value, the chart carries a text alternative naming the top slices and the total, and colour is never the sole carrier of meaning. |
| NFR-007 | Usability     | The chart is legible without a pointing device — every bar is labelled with its project and hours on the chart itself, not only in a hover tooltip.         |
| NFR-008 | Reliability   | A report is a read-only query; it never blocks, delays or fails a concurrent time-entry save (spec 001, NFR-002).                                          |

Project-wide quality requirements (transport security, logging, error presentation)
belong in arc42 Section 10 and are referenced, not restated here.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                    | Expected Behavior                                                                                                                  |
| ----- | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| EC-1  | Begin date equals end date                                                  | Valid: a one-day report. Inclusive ranges make this the natural "what did I do on Tuesday" query                                       |
| EC-2  | A range extending into the future, e.g. 2026-09-01 to 2026-12-31            | Accepted; the future days contribute nothing because future entries cannot exist (spec 001, FR-011). No warning is shown              |
| EC-3  | A range entirely in the future                                              | Accepted and empty; FR-020's empty result names the range                                                                            |
| EC-4  | A malformed or non-existent date in the address, e.g. `2026-02-30`          | The whole query is refused and the default range (FR-003) is shown with a message; a partly-applied filter is never silently used     |
| EC-5  | An address naming a project id that does not exist, or that the viewer may not see | That id is dropped from the filter, the report is computed from what remains, and the viewer is told the filter was narrowed. Existence is never disclosed by an error |
| EC-6  | A User opens a shared address carrying a person filter naming Bob           | The person filter is ignored entirely (FR-015, *a filter narrows, never widens*); the report shows Ann's own hours                    |
| EC-7  | Exactly 16 projects hold hours                                              | 15 bars plus "Other (1 project)" — singular. The rule is not relaxed for a tail of one; the table still lists all 16                  |
| EC-8  | Fewer than 16 projects hold hours                                           | No "Other" slice is drawn at all                                                                                                     |
| EC-9  | One project holds 100% of the hours                                         | A single bar at full width showing 100.0%; not treated as an error or a degenerate chart                                             |
| EC-10 | Shares round to 99.9% or 100.1% across many slices                          | The shares are displayed as computed and the total carries a note that shares are rounded to one decimal. The *hours* always reconcile exactly (NFR-004); only the percentages may not |
| EC-11 | An entry is saved by someone else while the report is on screen             | The report is not live; it shows the data as of when it was computed. Re-running it is the refresh. No stale-data warning is shown    |
| EC-12 | A project is renamed between two runs of the same report                    | The new name is shown against the same hours. Labels resolve at read time; the report is not a snapshot                              |
| EC-13 | A project filter selecting projects that all hold zero hours                | An empty result (FR-020) naming the range and the filter, not an error                                                               |
| EC-14 | Two projects share a name (story 008 does not require uniqueness)           | Both appear as separate slices keyed by project id, disambiguated by their client column. They are never merged                       |
| EC-15 | A person has the User role and is assigned to no projects                   | The report is empty for every range; the empty state points at story 009 (ask a manager for an assignment) rather than showing a bare chart |
| EC-16 | The session expires while the report is on screen                           | Spec 007, EC-12 applies: redirected to sign-in, returned to the same report address afterwards with filters intact (FR-018)           |
| EC-17 | A range of exactly 366 days, and one of 367                                 | 366 is accepted, 367 refused (FR-006). The boundary is inclusive                                                                     |
| EC-18 | A deactivated person is selected in a Manager's person filter               | Allowed and reported normally; deactivated people remain selectable because their history is the reason to select them (EC / FR-017)  |

## 8. Success Criteria

<!-- Success criteria are prefixed SUC- to avoid colliding with the SC- acceptance
     scenario identifiers in Section 4. -->

| ID      | Criterion                                                                                                                             |
| ------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All acceptance scenarios SC-001 to SC-019 pass automatically in CI                                                                      |
| SUC-002 | A property-based test over generated entry sets confirms that the slice minutes sum exactly to the total and that every entry lands in exactly one slice |
| SUC-003 | For every generated report, the displayed grand total equals the sum of the displayed slice hours to two decimal places                 |
| SUC-004 | Replaying every report address as each of the three roles discloses no hour the role may not see, verified by comparing against a direct query |
| SUC-005 | A manager answers "how did last month divide across projects" in under 15 seconds from arriving on the screen, with no export           |
| SUC-006 | The report screen issues no write of any kind, verified by asserting an unchanged store across the full scenario suite                  |
| SUC-007 | NFR-001 is met against a seeded data set of 1.5 M entries                                                                              |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 001 (daily time entry)** — supplies `TimeEntry`, the duration and rounding
  rules, and the *totals are derived* invariant that this spec extends. Nothing to
  report before it exists.
- **Spec 007 (authentication and roles)** — supplies the signed-in Person and the
  scoping of FR-013/FR-014. The capability matrix in its Section 2.1 is the authority.
- **Story 008 (projects)** — supplies `name`, `client` and `isActive`. If `client` is
  not yet modelled when this ships, FR-009's client column is omitted and everything
  else stands.
- **Story 009 (project assignment)** — not a dependency. Assignment governs what a
  person may *book to*, not what they may *report on*; a User reports on their own
  entries wherever those entries point.
- **Story 013 (pivot)** extends `ReportQuery` with a dimension and `ReportSlice` with a
  non-project key. This spec's Section 5 must not be read as project-only by accident.
- **Story 014 (drill-through)** depends on FR-018's addressable query and on the
  *slices reconcile* invariant — a drill-through that does not total to its slice is a
  bug in one of the two features.
- **Story 016 (export)** exports this result; it must reuse this computation rather than
  re-derive it, or the file and the screen will eventually disagree.

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10; root namespace `pmsa`.
- Hours only — the system has no rates and no currency (Section 1.3).
- Wall-clock local dates with no timezone handling, inherited from spec 001, Section 9.2.
  A range means the same thing to everyone because everyone is in one timezone.
- **Accepted tension:** the 366-day cap (FR-006) makes "the last three years across all
  projects" unanswerable on this screen. It was chosen to keep NFR-001 achievable
  without pre-aggregation, which would break the *nothing is stored* invariant. If
  multi-year reporting is asked for, it is a new story with a stored roll-up and its own
  staleness rules — not a raised limit here. Recorded in arc42 Section 11.
- **Accepted tension:** the report is computed on demand and is not live (EC-11). A
  manager and a developer looking at "September" a minute apart can see different
  numbers if an entry was saved between them. Caching or snapshotting would fix the
  discrepancy and reintroduce staleness, which is worse.

### 9.3 Architecture References

All arc42 sections are currently empty templates. The table states what this spec
expects each to say once written.

| Arc42 Section                | Relevance to This Feature                                                                                                    |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| 03. Context & Scope          | Reporting is wholly internal; no external system consumes these figures in this slice (story 016 adds a file, not an interface) |
| 05. Building Block View      | Introduces the reporting building block: a read-only query side over the `TimeEntry` store that specs 013–016 all sit on         |
| 06. Runtime View             | The report sequence: resolve role scope, build the query, aggregate in the store, shape slices, render chart and table          |
| 08. Cross-cutting Concepts   | Aggregation and rounding rules; the addressable-query convention (FR-018) that makes every report shareable and drillable        |
| 09. Architecture Decisions   | Needs ADRs for: computing reports on demand rather than pre-aggregating; bounding ranges at 366 days; carrying query state in the address |
| 10. Quality Requirements     | NFR-001 and NFR-003 are the project's first reporting quality scenarios; promote the response-time targets here                 |
| 11. Risks & Technical Debt   | The 366-day cap; non-live reports; the scan risk behind NFR-003 if indexing is left to chance                                   |
| 12. Glossary                 | Report, range, slice, share, grand total, scope, dimension (013), drill-through (014)                                           |

## 10. Open Questions

| #   | Question                                                                                          | Owner        | Status   | Resolution                                                                                                       |
| --- | --------------------------------------------------------------------------------------------------- | ------------ | -------- | ------------------------------------------------------------------------------------------------------------------ |
| 1   | Does story 008 model `client` as a first-class attribute, and should it become a grouping dimension? | Product      | Open     | Not a blocker: FR-009 degrades to omitting the column. If it becomes a dimension it is an addition to spec 013     |
| 2   | Is 366 days the right ceiling, or is "this year vs last year" a real question users will bring?     | Product      | Open     | Revisit after first use. The interim answer is story 016 (export) for anything longer                             |
| 3   | Should a User be able to see project-wide totals for projects they are assigned to, as spec 007's matrix hints for story 015? | Product | Resolved | No, not on this screen. Spec 007's matrix grants Users aggregate *consumption* in story 015 only; in this report a User sees their own hours and nothing else (FR-013) |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/002/003/007/016,
        US-002 SC-009/012/013/019, US-003 SC-010/011, US-004 SC-004/017/018,
        US-005 SC-014/015)
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
