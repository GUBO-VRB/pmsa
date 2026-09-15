# Feature Specification: Report Pivot Dimensions

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
| Feature ID      | 013                                                                      |
| Status          | Draft                                                                    |
| Author          | gunther bogaert                                                          |
| Created         | 2026-09-15                                                               |
| Last updated    | 2026-09-15                                                               |
| Epic / Parent   | Story map activity 4 — *Report & visualize*                              |
| Arc42 reference | 05 Building Block View, 06 Runtime View, 08 Cross-cutting Concepts, 10 Quality Requirements, 12 Glossary |

> **Scope note.** This spec extends the report screen defined by
> [spec 012](012-date-range-project-report.md) with a **dimension** — the axis the same
> selected hours are grouped along. It adds nothing to the filter, the scoping, the
> rounding or the chart-plus-table pairing; all of those are 012's and are not restated.
> Read 012 first. The story map calls story 013 "a first-class story rather than a
> variation of 012" because multi-angle reporting is the product's point of difference,
> and this spec honours that: the dimension is a peer of the range, not a toggle hidden
> in a menu.

> **Terminology.** **ReportQuery**, **ReportSlice**, **ReportResult**, *slices
> reconcile* and *nothing is stored* come from spec 012, Section 5. **Person** and the
> role matrix come from [spec 007](007-authentication-and-roles.md). **TimeEntry** comes
> from [spec 001](001-daily-time-entry.md).

### 1.1 Problem Statement

One breakdown answers one question. "Where did September go?" is a per-project
question, "is anyone carrying too much?" is a per-person question, and "was the work
steady or did it all land in the last week?" is a per-day question — and they are the
same hours viewed from three positions. A report that only groups one way forces the
other two questions out to a spreadsheet, which is precisely the failure of the tool
being replaced.

### 1.2 Goal

The same filtered set of hours can be turned to face project, person or time without
changing the question — one control, the range and filters untouched, the grand total
provably identical. Switching angle costs a click and no re-entry of anything, so
following a hunch across three angles is cheap enough to actually do.

### 1.3 Non-Goals

- Changing the filter, the scoping, the range presets or the rounding — all owned by
  spec 012
- Cross-tabulation: project × person in one grid. One dimension at a time, by decision
  recorded in Section 9.2
- Nesting or hierarchical grouping (client → project → person), and expandable rows.
  Drilling to *entries* is story 014; drilling to an intermediate level is not in the map
- Grouping by client, by role, or by any attribute other than the three the story names
- Stacked or multi-series charts — one series per report
- Comparing two dimensions, two ranges, or two people side by side
- Sorting the result by anything other than the rules in FR-008/FR-009
- Per-person targets, capacity, utilisation percentages or expected-hours baselines —
  nothing in the story map introduces a contracted working week, so "Ann logged 120
  hours" is never presented as a percentage of anything

## 2. User Stories

### US-001: Turn the same hours to face a different question

**As a** manager,
**I want** to switch the report between per project, per person and per day without
touching my filters,
**so that** following a hunch costs one click instead of a new query.

### US-002: See how the work is spread across people

**As a** manager,
**I want** the range's hours grouped per person,
**so that** I can see who carried what without opening eight individual timesheets.

### US-003: See the shape of the period over time

**As a** manager,
**I want** the range's hours grouped by time, including the days nobody logged,
**so that** gaps and crunches are visible rather than averaged away.

### US-004: Trust that it is the same data

**As a** manager,
**I want** the total to be provably identical whichever angle I pick,
**so that** I can quote a figure from any of the three without checking the others.

### US-005: See my own time-shape

**As a** developer,
**I want** the per-day angle over my own hours,
**so that** I can see which days I under-logged without opening the week view.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                       | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall offer a dimension control on the report screen with the values *Project*, *Person* and *Time*, defaulting to *Project*.                              | Must     | US-001     |
| FR-002 | The system shall group the selected time entries by the chosen dimension and sum their durations into one slice per distinct value.                                   | Must     | US-001     |
| FR-003 | The system shall preserve the range, the person filter and the project filter unchanged when the dimension changes.                                                   | Must     | US-001     |
| FR-004 | The system shall produce an identical grand total for every dimension over the same query.                                                                           | Must     | US-004     |
| FR-005 | The system shall carry the chosen dimension in the address of the report alongside the rest of the query (spec 012, FR-018).                                          | Must     | US-001     |
| FR-006 | The system shall label each *Person* slice with the person's full name, including people who are deactivated.                                                        | Must     | US-002     |
| FR-007 | The system shall omit the *Person* dimension entirely for the User role, since that role's report covers exactly one person (spec 007, FR-009 and FR-012).             | Must     | US-002     |
| FR-008 | The system shall order *Project* and *Person* slices by hours descending, ties broken by label ascending.                                                             | Must     | US-001     |
| FR-009 | The system shall order *Time* slices chronologically ascending, never by hours.                                                                                      | Must     | US-003     |
| FR-010 | The system shall include in the *Time* dimension every bucket in the range, including buckets holding zero hours.                                                     | Must     | US-003     |
| FR-011 | The system shall bucket the *Time* dimension by day for ranges up to 31 days, by ISO week for 32 to 184 days, and by calendar month beyond that.                       | Should   | US-003     |
| FR-012 | The system shall allow the automatic bucket granularity to be overridden to *Day*, *Week* or *Month*, and shall carry that choice in the address.                      | Should   | US-003     |
| FR-013 | The system shall render the *Time* dimension as a column chart on a continuous time axis, and *Project* and *Person* as horizontal bar charts (spec 012, FR-008).      | Should   | US-003     |
| FR-014 | The system shall label a *Time* slice unambiguously: a date for *Day*, the ISO week number with its begin and end dates for *Week*, and the month and year for *Month*.| Should   | US-003     |
| FR-015 | The system shall mark weekend buckets distinctly in the *Day* granularity, by a means other than colour alone.                                                        | Could    | US-003     |
| FR-016 | The system shall apply the 15-slice-plus-*Other* rule (spec 012, FR-012) to the *Project* and *Person* dimensions and never to the *Time* dimension.                   | Must     | US-003     |
| FR-017 | The system shall state the dimension in the empty result (spec 012, FR-020) — for *Time*, this means a chart of empty buckets rather than no chart.                    | Should   | US-003     |
| FR-018 | The system shall restrict the *Person* dimension's slices to the people the viewer's role and person filter permit, with no slice for anyone else.                     | Must     | US-002     |
| FR-019 | The system shall show the grand total, and for the *Time* dimension additionally the mean hours per non-empty bucket and the count of empty buckets.                   | Could    | US-003     |
| FR-020 | The system shall group by exactly one dimension at a time and shall offer no second grouping.                                                                        | Must     | US-001     |

**Conventions applied where the story left details open:** the story map's "per day"
dimension is named ***Time*** in the interface because FR-011 buckets it by week or
month on long ranges and calling it "day" would then be a lie; a *Person* slice is
keyed by person id, so two colleagues with the same full name remain two slices; the
mean in FR-019 excludes empty buckets, because a mean that includes weekends answers
no question anyone asked.

## 4. Acceptance Scenarios

### SC-001: Switching dimension keeps the question (FR-003, FR-004)

```gherkin
Given Mo is reporting on 2026-09-01 to 2026-09-30 filtered to "Apollo"
  And the grand total reads 168.00
When he switches the dimension to Person
Then the range is still 2026-09-01 to 2026-09-30
  And the project filter is still "Apollo"
  And the grand total still reads 168.00
```

### SC-002: Hours per person (FR-002, FR-006)

```gherkin
Given in September 2026 Ann logged 100.00 hours and Bob logged 68.00 hours
When Mo reports on September 2026 by Person
Then the result lists "Ann" 100.00 and "Bob" 68.00
  And the grand total reads 168.00
```

### SC-003: A leaver still has a slice (FR-006)

```gherkin
Given Bob logged 68.00 hours in September 2026
  And an Admin has since deactivated Bob
When Mo reports on September 2026 by Person
Then "Bob" appears with 68.00 hours
```

### SC-004: A User is not offered the Person dimension (FR-007)

```gherkin
Given Ann has the User role
When she opens the report screen
Then the dimension control offers Project and Time only
```

### SC-005: A User cannot reach the Person dimension by address (FR-007, FR-018)

```gherkin
Given Ann has the User role
When she opens a shared report address whose dimension is Person
Then the report is shown by Project
  And no other person's hours are disclosed
```

### SC-006: Time is ordered chronologically, not by size (FR-009)

```gherkin
Given 2026-09-01 holds 2.00 hours and 2026-09-02 holds 8.00 hours
When Ann reports on 2026-09-01 to 2026-09-02 by Time
Then 2026-09-01 is shown before 2026-09-02
```

### SC-007: Empty days are present (FR-010)

```gherkin
Given Ann logged hours on 2026-09-14 and 2026-09-16 but none on 2026-09-15
When she reports on 2026-09-14 to 2026-09-16 by Time
Then three buckets are charted
  And 2026-09-15 shows 0.00
```

### SC-008: Automatic granularity by day (FR-011)

```gherkin
Given the range is 2026-09-01 to 2026-09-30, a span of 30 days
When the report is shown by Time
Then it holds 30 daily buckets
```

### SC-009: Automatic granularity by week (FR-011, FR-014)

```gherkin
Given the range is 2026-04-01 to 2026-09-30, a span of 183 days
When the report is shown by Time
Then the buckets are ISO weeks
  And each is labelled with its week number and its begin and end dates
```

### SC-010: Automatic granularity by month (FR-011)

```gherkin
Given the range is 2025-10-01 to 2026-09-30, a span of 365 days
When the report is shown by Time
Then the buckets are calendar months
  And there are 12 of them
```

### SC-011: Overriding granularity (FR-012)

```gherkin
Given the range is 2026-09-01 to 2026-09-30, automatically bucketed by day
When Mo overrides the granularity to Week
Then the buckets are ISO weeks
  And the grand total is unchanged
```

### SC-012: Partial buckets at the edges of the range (FR-010, FR-014)

```gherkin
Given the range is 2026-09-03 to 2026-09-20 with granularity overridden to Week
When the report is shown
Then the first bucket covers 2026-09-03 to 2026-09-06 only
  And its hours exclude 2026-09-01 and 2026-09-02
```

### SC-013: The dimension travels in the link (FR-005)

```gherkin
Given Mo is reporting on September 2026 by Time at Week granularity
When he copies the address and Dee opens it
Then Dee sees the same range, by Time, at Week granularity
```

### SC-014: Every angle totals the same (FR-004)

```gherkin
Given a range holding 168.00 hours across 3 projects, 4 people and 22 days
When the report is shown by Project, then by Person, then by Time
Then each shows a grand total of 168.00
```

### SC-015: The long tail is not applied to Time (FR-016)

```gherkin
Given the range is 2026-09-01 to 2026-09-30 shown by Time at Day granularity
When the report is shown
Then all 30 buckets are charted
  And no "Other" bucket exists
```

### SC-016: The long tail is applied to Person (FR-016)

```gherkin
Given 20 people logged hours in the range
When Mo reports by Person
Then the chart holds 15 person bars and one "Other (5 people)" bar
  And the table lists all 20 people
```

### SC-017: A range with no hours, by Time (FR-017)

```gherkin
Given Ann logged nothing between 2026-07-01 and 2026-07-07
When she reports on that range by Time
Then seven empty buckets are charted
  And the grand total reads 0.00
```

### SC-018: The person filter narrows the Person dimension (FR-018)

```gherkin
Given Ann, Bob and Cy all logged hours in the range
When Mo filters the report to Ann and Bob and groups by Person
Then exactly two slices are shown
  And the grand total covers Ann and Bob only
```

### SC-019: A day's slice matches that day's screen (FR-002)

```gherkin
Given Ann's 2026-09-15 shows a daily total of 7.50 on the day screen
When she reports on 2026-09-15 to 2026-09-15 by Time
Then the single bucket reads 7.50
```

## 5. Domain Model

This feature introduces **no persisted entity**. It adds one attribute to spec 012's
`ReportQuery`, one to `ReportResult`, and generalises `ReportSlice`'s key.

### 5.1 Entities

None. Spec 012's *nothing is stored* invariant holds unchanged.

#### TimeEntry, Person, Project (referenced, not owned)

As spec 012, Section 5.1. The *Person* dimension additionally reads `Person.fullName`
and `Person.isActive`; the *Time* dimension reads only `TimeEntry.date`.

### 5.2 Relationships

- A **ReportQuery** now carries a **ReportDimension**; together they determine the
  partition of the selected **TimeEntry** set into **ReportSlice**s.
- A **TimeEntry** contributes to exactly one slice in *every* dimension. This is why
  FR-004 is provable rather than merely likely: three different total-and-disjoint
  partitions of one set have one sum.
- A **TimeBucket** is not an entity and holds no rows — it is an interval of the range,
  and a *Time* slice is the set of entries whose `date` falls in it. Buckets are
  generated from the range, which is why an empty one still exists (FR-010).

### 5.3 Value Objects

#### ReportDimension

| Attribute | Type | Constraints                                                                 |
| --------- | ---- | ----------------------------------------------------------------------------- |
| value     | enum | required, [Project, Person, Time], default Project; Person is unavailable to the User role |

#### TimeGranularity

| Attribute  | Type    | Constraints                                                        |
| ---------- | ------- | ------------------------------------------------------------------ |
| value      | enum    | required, [Day, Week, Month]                                        |
| isExplicit | boolean | required; false means derived from the range length by FR-011        |

Relevant only when the dimension is *Time*; carried in the address regardless so that
a link keeps an overridden granularity (SC-013).

#### TimeBucket

| Attribute | Type      | Constraints                                                                  |
| --------- | --------- | ---------------------------------------------------------------------------- |
| begin     | date      | required, within the report range                                             |
| end       | date      | required, within the report range, not before `begin`                          |
| label     | string    | required, unambiguous per FR-014                                              |

Buckets tile the range exactly: contiguous, non-overlapping, together covering every
date in it. The first and last may be shorter than a full week or month (SC-012).

#### ReportSlice (extended from spec 012)

| Attribute | Type            | Constraints                                                                       |
| --------- | --------------- | --------------------------------------------------------------------------------- |
| key       | UUID or date    | required; a project id, a person id, or the begin date of a TimeBucket             |
| label     | string          | required; project name, person full name, or bucket label                          |
| minutes   | integer         | required, >= 0 — **zero is permitted for the Time dimension only** (FR-010)        |

The widened key is what story 014 drills on, and the widened `minutes >= 0` is the one
place this spec deliberately relaxes a rule of spec 012.

### 5.4 Domain Rules and Invariants

- **One set, three partitions**: the dimension changes how the selected entries are
  grouped and never which entries are selected. Filtering and scoping happen before
  grouping, always.
- **The total is dimension-invariant**: the grand total of a query is identical under
  *Project*, *Person* and *Time*. If two angles disagree, an entry has been dropped or
  double-counted and both reports are wrong — not just the smaller one.
- **Buckets tile the range**: *Time* buckets are contiguous, non-overlapping, and cover
  every date in the range including its first and last, even partially.
- **An empty bucket is data**: zero hours on a Wednesday is the finding, so *Time*
  slices are never suppressed for being zero. This deliberately breaks spec 012's *zero
  is absent* rule, which remains in force for *Project* and *Person*.
- **Time is ordered by time**: *Time* slices are never re-ordered by size. A time axis
  sorted by magnitude is not a time axis.
- **Slices are keyed by identity, not by label**: two people or two projects sharing a
  name remain two slices. Labels are for reading; keys are for correctness.
- **A dimension is not a permission**: choosing *Person* grants no visibility. What a
  viewer may see is already settled by spec 007 before the dimension is applied, which
  is why FR-007's omission of the control is a usability measure and FR-018 is the rule.
- **Granularity does not change the answer**: re-bucketing *Time* redistributes the same
  minutes. Every granularity of the same query has the same grand total.
- **One grouping at a time**: there is no second dimension, so no slice is ever a
  sub-slice of another.

## 6. Non-Functional Requirements

| ID      | Category      | Requirement                                                                                                                                       |
| ------- | ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance   | Switching dimension re-renders within spec 012, NFR-002 (< 1 s at p95) and re-queries at most once; no dimension is slower than another by more than 25%. |
| NFR-002 | Performance   | The *Time* dimension at *Day* granularity over the maximum 366-day range renders in < 2.5 s at p95 despite producing 366 buckets.                    |
| NFR-003 | Correctness   | The grand totals of the three dimensions over one query are bit-identical in minutes, verified continuously rather than by inspection.                |
| NFR-004 | Accessibility | Every dimension's result is fully readable from its table (spec 012, NFR-006). The *Time* chart additionally exposes each bucket's label and value as text, and weekend marking (FR-015) never relies on colour alone. |
| NFR-005 | Usability     | The dimension control is visible without scrolling and its current value is readable at a glance; switching requires one interaction, not a menu traversal. |
| NFR-006 | Security      | The *Person* dimension discloses no person who has no hours in the permitted, filtered set — the slice list is not a directory of the organisation.  |

Project-wide quality requirements belong in arc42 Section 10 and are referenced here,
not restated.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                             | Expected Behavior                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | An unknown dimension value arrives in the address                                    | Falls back to *Project* with a message; the rest of the query is honoured (spec 012, EC-4 is not triggered — only the dimension is invalid) |
| EC-2  | The dimension is *Person* and the viewer's role drops to User mid-session            | The next request is served by *Project*; no person-level data is disclosed (spec 007, FR-017 takes effect per request)                     |
| EC-3  | A range of exactly 31 days, and one of 32                                            | 31 buckets by Day; 32 days buckets by Week. The boundary is inclusive at 31                                                               |
| EC-4  | A range of exactly 184 days, and one of 185                                          | 184 by Week; 185 by Month. The boundary is inclusive at 184                                                                               |
| EC-5  | *Day* granularity is forced on a 366-day range                                       | Honoured (FR-012) with 366 buckets, subject to NFR-002. The chart labels every nth bucket rather than dropping buckets — the data is complete even where the axis is thinned |
| EC-6  | *Month* granularity is forced on a 3-day range                                       | Honoured: one bucket covering those 3 days, labelled as the month with its actual span stated                                             |
| EC-7  | A range beginning mid-week with Week granularity                                     | The first bucket is partial and is labelled with its real begin and end dates, never the full ISO week's (SC-012)                          |
| EC-8  | A range spanning a year boundary at Week granularity, e.g. ISO week 53 into week 1   | Buckets remain chronological and labelled with their dates; the ISO week number alone is never the key. Year-crossing weeks do not collide |
| EC-9  | Two people share the full name "Jan Peeters"                                         | Two slices, keyed by person id, disambiguated by email. They are never merged                                                             |
| EC-10 | A person has hours in range but is filtered out by the project filter                | No slice for them. Filters apply before grouping, so the Person dimension shows only people with hours *in the filtered set*               |
| EC-11 | Every bucket in the *Time* dimension is empty                                        | A chart of empty buckets and a total of 0.00 (FR-017), which reads as "nothing was logged all period" — more informative than a blank      |
| EC-12 | A single day holds more than 24 hours across several people                          | Correct and expected in the *Time* dimension once more than one person is in scope; no 24-hour ceiling applies (spec 001's ceiling is per person per day) |
| EC-13 | The *Other* slice on the Person dimension groups people the viewer may see           | It names the count only. Its members are named in the table (spec 012, FR-012), never hidden — the grouping is visual, not a privacy control |
| EC-14 | A back-dated entry lands in a bucket after the report was first read                 | The next run shows it. Spec 012, EC-11 applies unchanged                                                                                  |
| EC-15 | Granularity is overridden, then the range is changed so the override is inappropriate | The override is kept (it is explicit) and the bucket count is recomputed. A note states that the granularity is overridden and offers a reset to automatic |
| EC-16 | A daylight-saving weekend at *Day* granularity                                       | Buckets are calendar days, not 24-hour spans, so DST is irrelevant here (spec 001, EC-14 covers time storage)                              |

## 8. Success Criteria

<!-- Success criteria are prefixed SUC- to avoid colliding with the SC- acceptance
     scenario identifiers in Section 4. -->

| ID      | Criterion                                                                                                                            |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All acceptance scenarios SC-001 to SC-019 pass automatically in CI                                                                     |
| SUC-002 | A property-based test over generated entry sets confirms the grand total is identical under all three dimensions and all three granularities |
| SUC-003 | A property-based test confirms *Time* buckets tile the range: contiguous, non-overlapping, and covering every date exactly once        |
| SUC-004 | No request as the User role can produce a Person-dimensioned result, verified by replaying every dimension value against every role    |
| SUC-005 | A manager moves from a per-project answer to a per-person answer over the same range in one interaction, with no filter re-entry       |
| SUC-006 | For a range with known gaps, the *Time* dimension reports the same number of empty buckets as the data contains                        |
| SUC-007 | NFR-002 is met against a seeded data set of 1.5 M entries at 366 daily buckets                                                        |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 012 (date-range project report)** — supplies the screen, the filter, the
  scoping, the rounding, the chart-plus-table pairing and the addressable query. This
  spec is meaningless without it and must not duplicate any of it.
- **Spec 007 (authentication and roles)** — FR-007 and FR-018 implement its Section 2.1
  matrix row *"Reports per person: own hours only / ✅ / ✅"*.
- **Spec 001 (daily time entry)** — supplies `TimeEntry.date`, which is the whole of the
  *Time* dimension, and the daily total that SC-019 reconciles against.
- **Story 004 (week view)** overlaps conceptually with *Time* at *Day* granularity for a
  single person over a single week. They are not the same feature: 004 lists entries for
  correcting them, 013 aggregates buckets for reading a shape. If they ever disagree on
  a day's total, 001's *totals are derived* invariant has been broken somewhere.
- **Story 014 (drill-through)** must drill every dimension, including an empty *Time*
  bucket, which drills to an empty list rather than to nothing.
- **Story 016 (export)** exports whichever dimension is on screen, with the dimension
  named in the file.

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10; root namespace `pmsa`.
- ISO-8601 weeks beginning Monday, inherited from spec 012, FR-004.
- **Accepted tension:** one dimension at a time (FR-020) means "how much did Ann spend
  on Apollo in September" needs a filter plus a dimension rather than a cross-tab. It
  was chosen because a cross-tab is a spreadsheet, and rebuilding a spreadsheet is the
  failure this product exists to avoid; filter-then-group answers the same question with
  a simpler screen and a simpler invariant. Recorded in arc42 Section 11 as the first
  thing to revisit if users ask for a grid.
- **Accepted tension:** naming the story map's "per day" dimension ***Time*** and
  auto-bucketing it (FR-011) breaks a literal reading of story 013. A literal per-day
  chart over a 366-day range is 366 unreadable bars, so the choice is between a dimension
  that degrades and one that lies about its own granularity. FR-012's override keeps the
  literal behaviour reachable.
- **Accepted tension:** hiding the *Person* dimension from Users (FR-007) means the
  dimension control differs by role, so two people looking at the same link see
  different controls. Spec 007's FR-009 mandates exactly this, and SC-005 keeps it from
  becoming a security measure by accident.

### 9.3 Architecture References

All arc42 sections are currently empty templates. The table states what this spec
expects each to say once written.

| Arc42 Section                | Relevance to This Feature                                                                                                 |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| 05. Building Block View      | Extends spec 012's reporting building block with a dimension strategy; it is one query path, not three report screens        |
| 06. Runtime View             | Scope → filter → select → partition by dimension → order → render. The partition step is the only thing this spec inserts    |
| 08. Cross-cutting Concepts   | The dimension-invariant-total rule, which every future aggregate feature (015, 016) must preserve                            |
| 09. Architecture Decisions   | Needs ADRs for: one dimension rather than cross-tabulation; automatic time granularity with an explicit override             |
| 10. Quality Requirements     | NFR-003 is a correctness scenario worth promoting project-wide: aggregates must agree across presentations                   |
| 11. Risks & Technical Debt   | No cross-tab; the Time/day naming divergence from the story map; dimension controls differing by role                        |
| 12. Glossary                 | Dimension, pivot, slice, bucket, granularity, Time dimension (vs. the story map's "per day")                                 |

## 10. Open Questions

| #   | Question                                                                                          | Owner   | Status   | Resolution                                                                                                        |
| --- | --------------------------------------------------------------------------------------------------- | ------- | -------- | ------------------------------------------------------------------------------------------------------------------- |
| 1   | Should *Client* become a fourth dimension once story 008 settles whether client is a first-class attribute? | Product | Open | Additive: a fourth enum value and one more slice label rule. Nothing in this spec forecloses it                    |
| 2   | Does the story map's story 013 wording need reconciling to the *Time* dimension name?               | Product | Open     | Recommended. The map says "per day"; the interface says "Time" with a day granularity. Section 9.2 records why      |
| 3   | Is a per-person capacity baseline (e.g. 40 h/week) wanted, turning the Person dimension into utilisation? | Product | Deferred | Out of scope (Section 1.3) — no contracted working week exists anywhere in the map. It would be a new story, not an NFR here |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/013/014, US-002
        SC-002/003/004/005/016/018, US-003 SC-006/007/008/009/010/011/012/015/017,
        US-004 SC-001/014, US-005 SC-019)
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
