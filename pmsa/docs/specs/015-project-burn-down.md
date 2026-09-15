# Feature Specification: Project Burn-Down

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
| Feature ID      | 015                                                                      |
| Status          | Draft                                                                    |
| Author          | gunther bogaert                                                          |
| Created         | 2026-09-15                                                               |
| Last updated    | 2026-09-15                                                               |
| Epic / Parent   | Story map activity 4 — *Report & visualize*                              |
| Arc42 reference | 05 Building Block View, 06 Runtime View, 08 Cross-cutting Concepts, 09 Architecture Decisions, 10 Quality Requirements, 12 Glossary |

> **Scope note.** This spec covers story **015** — budgeted hours versus logged hours,
> percentage consumed, and a warning as a project nears its budget. It is deliberately
> **not** a variant of the report screen ([spec 012](012-date-range-project-report.md)):
> a report answers *what happened in a range*, a burn-down answers *how much of a
> project's envelope is gone*, and those two questions are scoped differently on purpose
> (see the *Consumption is lifetime-to-date* invariant). It reuses 012's rounding and
> scoping rules and [spec 014](014-chart-drill-through.md)'s drill, and restates neither.

> **Terminology.** **TimeEntry** and *totals are derived* come from
> [spec 001](001-daily-time-entry.md). **Person**, **Role** and the capability matrix
> come from [spec 007](007-authentication-and-roles.md). **Project** and its `budgetHours`
> are owned by **stories 008 and 010**; this spec consumes them and specifies what the
> budget *means*, not how it is set.

### 1.1 Problem Statement

A project sold as 400 hours is being consumed by a dozen people a few hours at a time,
and today the only way to know how much is left is to add it up by hand — which means
it is discovered at the end, when the answer is "we passed it three weeks ago". Hours
are already being logged accurately enough to answer the question continuously; nothing
in the system turns them into the one number that decides whether a project is in
trouble.

### 1.2 Goal

Every project with a budget shows, at any moment, how many hours it was given, how many
have been logged against it, what fraction that is, and how much is left — with the
projects that are approaching or past their budget visibly distinguished from the ones
that are not, and with a curve showing how they got there. The number is always live,
always derived from the entries, and always reconcilable down to a single time entry.
This is the story that makes the app worth opening when you are not logging hours.

### 1.3 Non-Goals

- Setting, changing or validating a budget — that is story 010. This spec reads
  `budgetHours` and never writes it
- Creating, editing or archiving projects — story 008
- **Money.** A budget is an envelope of *hours*. There are no rates, no costs, no
  revenue and no margin anywhere in the system (spec 012, Section 1.3)
- Email, chat or push notification of a warning. No email infrastructure exists
  (spec 007, Section 9.2), so every warning in this spec is something seen on a screen
- Blocking, refusing or warning at the point of time entry. The fast path of
  [spec 001](001-daily-time-entry.md) is not touched — see Section 9.2
- Per-project or configurable warning thresholds; the bands in FR-006 are fixed
- Budget *history* — who changed a budget, when, and what it was before. Consumption is
  always measured against the budget as it stands now (see Open Question 2)
- Per-person budgets, per-phase or per-milestone budgets, and budget allocation between
  people
- Scheduling, capacity planning or resourcing — knowing a project will overrun does not
  imply the system knows what to do about it
- Restricting the burn-down to a date range. Explicitly refused; see the
  *Consumption is lifetime-to-date* invariant

## 2. User Stories

### US-001: Know how much of the envelope is gone

**As a** manager,
**I want** to see a project's budgeted hours, logged hours, percentage consumed and
hours remaining,
**so that** I know where a project stands without adding anything up.

### US-002: Be told before it is too late

**As a** manager,
**I want** projects nearing or past their budget to stand out from the ones that are
fine,
**so that** I find out at 90% rather than at 130%.

### US-003: See the shape of the consumption

**As a** manager,
**I want** a curve of cumulative hours against the budget over the project's life,
**so that** I can tell a project that is accelerating from one that is merely long.

### US-004: Scan the whole portfolio

**As a** manager,
**I want** every budgeted project's status on one screen,
**so that** checking the health of everything is one visit, not one per project.

### US-005: Explain a figure

**As a** manager,
**I want** to open the consumed figure and reach the entries behind it,
**so that** "we are at 96%" can be defended rather than merely asserted.

### US-006: Know how the project I work on is doing

**As a** developer,
**I want** to see the consumption of projects I am assigned to,
**so that** I know whether the hours I am about to log are into a healthy budget or
past the end of one.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                              | Priority | User Story |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall show, for a project with a budget, its budgeted hours, its consumed hours, its percentage consumed and its remaining hours.                                | Must     | US-001     |
| FR-002 | The system shall compute consumed hours as the sum of the durations of every time entry on that project, by every person, dated on or before the as-of date.                | Must     | US-001     |
| FR-003 | The system shall default the as-of date to the current date and allow it to be moved earlier, recomputing every figure as at that date.                                     | Should   | US-003     |
| FR-004 | The system shall compute remaining hours as budgeted minus consumed, and shall present a negative result as an overrun rather than as zero.                                 | Must     | US-001     |
| FR-005 | The system shall compute percentage consumed as consumed divided by budgeted, presented to one decimal place, with no upper bound.                                          | Must     | US-001     |
| FR-006 | The system shall assign each budgeted project a status of *Healthy* below 75%, *Approaching* from 75% to below 90%, *Critical* from 90% to below 100%, and *Over budget* at 100% and above. | Must | US-002 |
| FR-007 | The system shall derive the status from the unrounded percentage, so that a project displaying 90.0% through rounding of 89.96% is *Approaching*, not *Critical*.            | Should   | US-002     |
| FR-008 | The system shall convey status by a means other than colour alone, so that it survives greyscale and colour-blindness.                                                      | Must     | US-002     |
| FR-009 | The system shall present a project with no budget as *No budget set*, showing its consumed hours and no percentage, no remaining figure and no status.                       | Must     | US-001     |
| FR-010 | The system shall list every project the viewer is permitted to see with its status, consumed, budgeted and percentage, ordered by percentage consumed descending.            | Must     | US-004     |
| FR-011 | The system shall place projects with no budget after all budgeted projects in that list, ordered by consumed hours descending.                                              | Should   | US-004     |
| FR-012 | The system shall allow the list to be filtered to *Approaching or worse*, and to exclude archived projects, defaulting to excluding them.                                    | Should   | US-004     |
| FR-013 | The system shall chart cumulative consumed hours over the project's life against a horizontal line at the budgeted hours.                                                  | Must     | US-003     |
| FR-014 | The system shall begin that chart at the project's first logged entry and end it at the as-of date.                                                                        | Must     | US-003     |
| FR-015 | The system shall draw, where the project has a start date and an end date, a straight reference line from zero at the start date to the budget at the end date.              | Should   | US-003     |
| FR-016 | The system shall state, for a project that is *Approaching* or *Critical*, the date on which the budget will be exhausted if the mean rate of the last 28 days continues, or that it will not be exhausted where that rate is zero. | Could | US-002 |
| FR-017 | The system shall grant the Manager and Admin roles the burn-down of every project (spec 007, FR-011).                                                                       | Must     | US-004     |
| FR-018 | The system shall grant the User role the burn-down of projects they are assigned to, showing project-wide totals only and no breakdown by person (spec 007, FR-012).          | Must     | US-006     |
| FR-019 | The system shall show, for the Manager and Admin roles, the consumed hours broken down per person on the project.                                                          | Should   | US-005     |
| FR-020 | The system shall allow the Manager and Admin roles to open the consumed figure and reach the time entries behind it (spec 014), and shall offer the User role that drill over their own entries only. | Should | US-005 |
| FR-021 | The system shall include the entries of deactivated people and count hours logged to a project after it was archived (spec 007, FR-016).                                     | Must     | US-001     |
| FR-022 | The system shall compute every figure from the time entries at request time and store no consumed total, percentage or status.                                              | Must     | US-001     |
| FR-023 | The system shall show a budgeted project with no logged entries as 0.00 consumed, 0.0%, *Healthy*, with the whole budget remaining.                                         | Should   | US-001     |
| FR-024 | The system shall offer no control on the burn-down that creates, changes or deletes a time entry, a project or a budget.                                                    | Must     | US-001     |

**Conventions applied where the story left details open:** a budget is a *lifetime*
envelope of hours for the whole project and all its people, so consumption is never
scoped to a date range (FR-002); the as-of date (FR-003) moves the observation point
backwards through history and is not a range filter; percentages are presented to one
decimal place and statuses derived from the unrounded value; the "warning as it nears
the budget" of story 015 is the *Approaching* and *Critical* status shown on screen —
nothing is sent anywhere, because nothing can be.

## 4. Acceptance Scenarios

### SC-001: The four figures (FR-001, FR-002, FR-004, FR-005)

```gherkin
Given "Apollo" has a budget of 400.00 hours
  And 310.00 hours have been logged against it by everyone, ever
When Mo views its burn-down
Then it shows 400.00 budgeted, 310.00 consumed, 77.5% and 90.00 remaining
```

### SC-002: Consumption is not scoped to a range (FR-002)

```gherkin
Given "Apollo" has 300.00 hours logged before 2026-09-01 and 10.00 hours in September
When Mo views its burn-down on 2026-09-30
Then consumed reads 310.00
  And no date-range control restricts that figure
```

### SC-003: Everybody's hours count (FR-002)

```gherkin
Given Ann logged 200.00 hours and Bob logged 110.00 hours on "Apollo"
When Mo views its burn-down
Then consumed reads 310.00
```

### SC-004: Overrun is shown as overrun (FR-004, FR-005, FR-006)

```gherkin
Given "Atlas" has a budget of 100.00 hours and 130.00 hours logged
When Mo views its burn-down
Then it shows 130.0% consumed
  And 30.00 hours over budget
  And the status is "Over budget"
```

### SC-005: The status bands (FR-006)

```gherkin
Given a budget of 100.00 hours
When 74.99, 75.00, 89.99, 90.00, 99.99 and 100.00 hours are consumed in turn
Then the statuses are Healthy, Approaching, Approaching, Critical, Critical
     and Over budget respectively
```

### SC-006: Rounding does not promote a status (FR-007)

```gherkin
Given a budget of 100.00 hours and 89.96 hours consumed
When the burn-down is shown
Then the percentage displays as 90.0%
  And the status is "Approaching"
```

### SC-007: A project without a budget (FR-009)

```gherkin
Given "Hermes" has no budget and 42.00 hours logged
When Mo views its burn-down
Then it shows 42.00 consumed and "No budget set"
  And no percentage, remaining figure or status is shown
```

### SC-008: A budgeted project with no hours yet (FR-023)

```gherkin
Given "Zeus" has a budget of 200.00 hours and no entries
When Mo views its burn-down
Then it shows 0.00 consumed, 0.0%, 200.00 remaining and the status "Healthy"
```

### SC-009: The portfolio list (FR-010, FR-011)

```gherkin
Given "Atlas" is at 130.0%, "Apollo" at 77.5%, "Zeus" at 0.0%
  And "Hermes" has no budget and 42.00 hours
When Mo views the project list
Then the order is "Atlas", "Apollo", "Zeus", then "Hermes"
```

### SC-010: Filtering to what needs attention (FR-012)

```gherkin
Given the list holds projects at 130.0%, 92.0%, 77.5% and 12.0%
When Mo filters to "Approaching or worse"
Then three projects are listed
  And the project at 12.0% is not among them
```

### SC-011: Archived projects are out of the way by default (FR-012)

```gherkin
Given "Apollo" is archived at 99.0% consumed
When Mo views the project list
Then "Apollo" is not listed
  And including archived projects brings it back with its figures intact
```

### SC-012: The curve against the budget (FR-013, FR-014)

```gherkin
Given "Apollo" has a budget of 400.00 hours and entries from 2026-03-02 onward
When Mo views its burn-down as at 2026-09-15
Then the curve begins at 2026-03-02 and ends at 2026-09-15
  And it rises to 310.00
  And a horizontal line marks 400.00
```

### SC-013: Looking at the project as it stood earlier (FR-003)

```gherkin
Given "Apollo" had 250.00 hours logged on or before 2026-08-31
  And has 310.00 hours logged today
When Mo sets the as-of date to 2026-08-31
Then consumed reads 250.00 and the percentage reads 62.5%
  And the curve ends at 2026-08-31
```

### SC-014: The reference line (FR-015)

```gherkin
Given "Apollo" runs from 2026-03-01 to 2026-12-31 with a budget of 400.00 hours
When its burn-down is shown
Then a straight line runs from 0.00 at 2026-03-01 to 400.00 at 2026-12-31
```

### SC-015: No reference line without dates (FR-015)

```gherkin
Given "Hermes" has a budget but no start or end date
When its burn-down is shown
Then the cumulative curve and the budget line are drawn
  And no reference line is drawn
```

### SC-016: Projecting exhaustion (FR-016)

```gherkin
Given "Apollo" is at 77.5% of a 400.00-hour budget
  And 56.00 hours were logged in the last 28 days
When its burn-down is shown
Then it states that the budget will be exhausted around 2026-10-30
     if the current rate continues
```

### SC-017: No projection from a stalled project (FR-016)

```gherkin
Given "Apollo" is at 77.5% and no hours were logged in the last 28 days
When its burn-down is shown
Then it states that at the current rate the budget will not be exhausted
```

### SC-018: A Manager sees the per-person breakdown (FR-019)

```gherkin
Given Ann logged 200.00 and Bob logged 110.00 hours on "Apollo"
When Mo views its burn-down
Then Ann 200.00 and Bob 110.00 are listed
  And they sum to the consumed figure of 310.00
```

### SC-019: A User sees totals but not people (FR-018)

```gherkin
Given Ann has the User role and is assigned to "Apollo"
When she views its burn-down
Then she sees 400.00 budgeted, 310.00 consumed, 77.5% and the status
  And no breakdown attributing hours to Bob
```

### SC-020: A User cannot see an unassigned project's burn-down (FR-018)

```gherkin
Given Ann has the User role and is not assigned to "Zeus"
When she requests "Zeus"'s burn-down by its address directly
Then the request is refused as not permitted
  And no budget or consumption figure is disclosed
```

### SC-021: Drilling into the consumed figure (FR-020)

```gherkin
Given "Apollo" shows 310.00 hours consumed
When Mo opens that figure
Then the time entries behind it are listed
  And their total reads 310.00
```

### SC-022: A User's drill is limited to their own entries (FR-020)

```gherkin
Given Ann has the User role, is assigned to "Apollo", and logged 200.00 of its 310.00 hours
When she opens the consumed figure
Then only her own 200.00 hours of entries are listed
  And she is told the list covers her own hours, not the project's 310.00
```

### SC-023: A leaver's hours stay consumed (FR-021)

```gherkin
Given Bob logged 110.00 hours on "Apollo"
When an Admin deactivates Bob
Then "Apollo" still shows 310.00 consumed and 77.5%
```

### SC-024: The burn-down changes nothing (FR-024)

```gherkin
Given Mo is viewing "Apollo"'s burn-down
When he inspects the screen
Then no control changes the budget, the project or any entry
```

## 5. Domain Model

This feature introduces **no persisted entity** and adds **no attribute** to any
existing one. `budgetHours` belongs to `Project` and is written by story 010; everything
else on this screen is derived from `TimeEntry` at request time.

### 5.1 Entities

None of its own.

#### Project (referenced, not owned)

Defined by story 008, with `budgetHours` added by story 010. This spec reads the
following and writes nothing. The constraints stated here are what this feature
*relies on*; story 010 is the authority for enforcing them.

| Attribute    | Type    | Constraints                          | Description                                                                 |
| ------------ | ------- | ------------------------------------ | ----------------------------------------------------------------------------- |
| id           | UUID    | PK                                   |                                                                               |
| name         | string  | required                             | Displayed on the burn-down and in the list                                    |
| client       | string  | optional                             | Label only; never an access boundary (spec 007, Section 1.3)                   |
| isActive     | boolean | required                             | Archived projects are excluded from the list by default (FR-012), never from their own figures |
| budgetHours  | decimal | optional, > 0 when set               | The lifetime envelope. Absent means *no budget set* (FR-009); zero is not a budget |
| startDate    | date    | optional                             | Used only for the reference line (FR-015)                                     |
| endDate      | date    | optional, not before `startDate`     | Used only for the reference line (FR-015)                                     |

#### TimeEntry (referenced, not owned)

Defined by [spec 001](001-daily-time-entry.md). This spec reads `projectId`, `date`,
`personId` and the derived `duration`. It never writes.

#### Person (referenced, not owned)

Defined by [spec 007](007-authentication-and-roles.md). Read for `fullName` in FR-019's
breakdown and for the role that scopes FR-017/FR-018.

### 5.2 Relationships

- A **Project** has at most one budget, expressed as `budgetHours` on the project
  itself. There is no separate Budget entity, because a budget with no identity, no
  history and no sub-allocation is an attribute, not a thing.
- A **Project** has many **TimeEntry** records; consumption is a calculation over all of
  them, unfiltered by person and unbounded in time.
- A **BurnDown** is not stored. It is the answer computed for one Project as at one
  date, and is discarded with the request.
- A **Person** is assigned to many **Project**s (story 009). This spec is the **only**
  feature that reads that assignment as a *visibility* rule (FR-018) rather than a
  booking rule — see the caveat in Section 5.4.

### 5.3 Value Objects

#### BurnDown

The complete answer for one project as at one date.

| Attribute       | Type            | Constraints                                                                          |
| --------------- | --------------- | -------------------------------------------------------------------------------------- |
| projectId       | UUID            | required                                                                                |
| asOf            | date            | required, defaults to the current date, never in the future                             |
| budgetedMinutes | integer         | optional, > 0 when present; absent means *no budget set*                                |
| consumedMinutes | integer         | required, >= 0                                                                          |
| status          | BudgetStatus    | present only when `budgetedMinutes` is present                                          |

`percentConsumed`, `remainingMinutes` and `status` are all **derived** from
`consumedMinutes` and `budgetedMinutes` and are never stored — storing any of them would
let them disagree with the entries.

#### BudgetStatus

| Attribute | Type | Constraints                                                                 |
| --------- | ---- | ----------------------------------------------------------------------------- |
| value     | enum | required, [Healthy, Approaching, Critical, OverBudget]; derived per FR-006/FR-007 |

The thresholds 75%, 90% and 100% are part of the domain in this slice, not configuration
(Section 1.3).

#### BurnDownPoint

One point on the curve of FR-013.

| Attribute        | Type    | Constraints                                       |
| ---------------- | ------- | ------------------------------------------------- |
| date             | date    | required, between the first entry and `asOf`       |
| cumulativeMinutes| integer | required, >= 0, non-decreasing across the series   |

#### ConsumptionRate

Backs the projection of FR-016.

| Attribute       | Type    | Constraints                                                                    |
| --------------- | ------- | ------------------------------------------------------------------------------ |
| windowDays      | integer | required, fixed at 28                                                           |
| minutesPerDay   | decimal | required, >= 0; the window's total minutes divided by 28, calendar days included |

Calendar days, not working days — the system has no notion of a working calendar, and
inventing one would make the projection depend on an assumption nobody stated.

### 5.4 Domain Rules and Invariants

- **Consumption is lifetime-to-date**: a project's consumed hours are every hour ever
  logged to it up to the as-of date, by everyone. This is *the* defining rule of the
  feature. A budget is an envelope for the whole project, so a range-scoped percentage
  would be arithmetic nonsense — "38% consumed in September" answers no question and
  invites the wrong decision. Anyone wanting hours in a range wants
  [spec 012](012-date-range-project-report.md), which is why the two screens are separate.
- **Everything is derived**: no consumed total, percentage or status is stored anywhere.
  This is spec 001's *totals are derived* invariant carried to its most consequential
  use — a stale burn-down is worse than none, because it is believed.
- **The curve is monotonic**: cumulative consumed hours never decrease as the date
  advances, because a time entry's contribution is never negative. A curve that dips
  means entries have been deleted, and the curve legitimately redraws lower next time;
  it never dips *within* a single rendering.
- **Over budget is a fact, not an error**: consumption above 100% is displayed as such,
  with the real percentage and a negative remainder shown as an overrun. Nothing clamps
  at 100%, and nothing prevents the hours from being logged.
- **The budget never blocks an entry**: no state of any budget refuses, warns on, or
  slows a time entry. The fast path of spec 001 is untouched (Section 9.2).
- **Status follows the unrounded value**: the band is computed before presentation
  rounding, so no project is ever badged into a band its displayed figure contradicts
  (FR-007).
- **A missing budget is not a zero budget**: absent `budgetHours` means no percentage
  exists at all. Treating it as zero would put every unbudgeted project at infinite
  consumption and at the top of the list.
- **Archiving and deactivation do not release hours**: hours logged by people who have
  left, and hours logged to projects since archived, remain consumed forever
  (spec 007, FR-016 and SUC-006). A burn-down that fell when someone left would be
  unusable as a record.
- **Assignment governs visibility here and nowhere else**: FR-018 scopes a User's
  burn-down to their assigned projects. This is the single exception to spec 007's
  *roles are global* invariant, and it is an exception that *narrows* — it grants a User
  nothing they would not otherwise have. It must not be generalised into a per-project
  permission model.
- **Aggregate visibility does not imply detail visibility**: a User may see that
  "Apollo" has consumed 310.00 hours without being permitted to see whose hours those
  are (FR-018, FR-020). The aggregate is the deliberate disclosure; the breakdown is not.
- **The projection is a statement about a rate, not a prediction**: FR-016 extrapolates
  one fixed window and says so in those terms. It is never presented as a forecast, a
  deadline, or an input to a decision the system takes on its own.
- **The burn-down is read-only**: no path through this feature writes anything.

## 6. Non-Functional Requirements

| ID      | Category      | Requirement                                                                                                                                                         |
| ------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance   | A single project's burn-down, including its curve over a three-year life, renders in < 1 s at p95.                                                                      |
| NFR-002 | Performance   | The portfolio list (FR-010) renders in < 1.5 s at p95 for 500 projects, computing 500 lifetime consumption figures without loading their entries.                       |
| NFR-003 | Scalability   | Lifetime consumption is aggregated in the store; no burn-down materialises a project's time entries, which may number in the tens of thousands (spec 001, NFR-006).      |
| NFR-004 | Correctness   | Consumed hours equal the sum of the drilled entries (spec 014, NFR-004) to two decimal places, and the per-person breakdown sums exactly to the consumed total.          |
| NFR-005 | Correctness   | Status is stable across renderings: the same data and the same as-of date always yield the same band, with no boundary flapping from rounding (FR-007).                  |
| NFR-006 | Security      | A User's burn-down request is refused for a project they are not assigned to, in the request itself and not merely by omission from the list (spec 007, FR-010).         |
| NFR-007 | Security      | No burn-down visible to the User role discloses another person's name, hours or existence.                                                                             |
| NFR-008 | Accessibility | Status is conveyed by text and shape as well as colour (FR-008); the curve's values are available as a table; the budget line and the reference line are distinguishable without colour. |
| NFR-009 | Reliability   | The burn-down is a read-only query and never blocks, delays or fails a concurrent time-entry save (spec 001, NFR-002).                                                   |
| NFR-010 | Usability     | A manager can tell, within 5 seconds of opening the portfolio list, which projects need attention — without reading a number.                                           |

Project-wide quality requirements belong in arc42 Section 10 and are referenced here,
not restated.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                          | Expected Behavior                                                                                                                             |
| ----- | ------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | A budget is reduced below the hours already consumed                                              | The project immediately reads over 100% and *Over budget*. Consumption is measured against the budget as it stands now; no history is kept (Open Question 2) |
| EC-2  | A budget is raised while a project is *Critical*                                                  | The status recomputes on the next request and may return to *Healthy*. Nothing records that it was ever *Critical*                             |
| EC-3  | A budget is removed entirely from a project with logged hours                                     | The project falls back to *No budget set* (FR-009) and moves to the unbudgeted section of the list (FR-011)                                    |
| EC-4  | `budgetHours` is set to zero, if story 010 permits it                                             | Treated as *No budget set*, not as a zero envelope — a division by zero has no meaningful percentage. Story 010 should refuse zero at source   |
| EC-5  | Consumed is exactly 100.00% of the budget                                                         | *Over budget*, with 0.00 remaining. The band is inclusive at 100% (FR-006, SC-005)                                                             |
| EC-6  | Consumed is exactly 75.00% or exactly 90.00%                                                      | *Approaching* and *Critical* respectively; the lower bound of each band is inclusive                                                           |
| EC-7  | A project's only entries are dated after the as-of date                                           | 0.00 consumed as at that date, with the curve empty and a note that the project's first entry is later than the as-of date                     |
| EC-8  | The as-of date is set in the future                                                               | Refused and reset to today; future entries cannot exist (spec 001, FR-011), so a future as-of date is a misleading way of asking for today     |
| EC-9  | The as-of date is set before the project's first entry                                            | 0.00 consumed, 0.0%, *Healthy*, with the whole budget remaining and an empty curve                                                            |
| EC-10 | A project has entries spread over three years                                                     | The curve is drawn at a granularity that keeps it legible (spec 013's granularity rules apply to its axis); the end value is always the exact consumed total, never a bucketed approximation |
| EC-11 | An entry is back-dated into a project's past                                                      | The curve redraws with the new point included and every subsequent cumulative value raised. The burn-down is current, not immutable (spec 012, EC-11) |
| EC-12 | An entry is deleted (story 005 / 011)                                                             | Consumption falls, the percentage falls, and the status may improve. No record is kept that it was ever higher                                 |
| EC-13 | Hours are logged to a project after it is archived (possible for entries back-dated before archiving) | They count (FR-021). Archiving stops booking, not accounting                                                                                  |
| EC-14 | Every person on a project is deactivated                                                          | Consumption is unchanged; the curve is flat from that point. The project is not marked complete — nothing infers completion from inactivity     |
| EC-15 | A project is *Approaching* with a 28-day window containing exactly zero hours                     | No date is projected; FR-016's "will not be exhausted at the current rate" wording is used. A division by a zero rate never produces an infinite or absent date |
| EC-16 | A projected exhaustion date falls after a project's `endDate`                                     | Stated as projected anyway, alongside the end date, without judgement. The system does not conclude that the project is fine                   |
| EC-17 | A User is assigned to a project and is later unassigned                                           | The burn-down becomes unavailable to them immediately (FR-018, NFR-006); their logged hours remain consumed and visible to managers            |
| EC-18 | A User is assigned to a project with no budget                                                    | They see its consumed total and *No budget set*; assignment grants no more than FR-018 allows                                                  |
| EC-19 | A User drills the consumed figure and their own hours are a small fraction of it                  | The drilled list shows their own hours with an explicit statement that it covers their entries, not the project's total (SC-022). The two figures are never presented as if they should match |
| EC-20 | 500 projects, of which 480 have no budget                                                         | The 20 budgeted projects head the list; the rest follow by consumed hours (FR-011). The list is not dominated by projects with nothing to report |
| EC-21 | The session expires while a burn-down is on screen                                                | Spec 007, EC-12 applies: redirected to sign-in, returned to the same burn-down afterwards                                                      |
| EC-22 | Two projects share a name                                                                         | Both listed, keyed by id and disambiguated by client (spec 012, EC-14). Their budgets and consumption are never merged                          |

## 8. Success Criteria

<!-- Success criteria are prefixed SUC- to avoid colliding with the SC- acceptance
     scenario identifiers in Section 4. -->

| ID      | Criterion                                                                                                                                       |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All acceptance scenarios SC-001 to SC-024 pass automatically in CI                                                                                |
| SUC-002 | A property-based test confirms that consumed hours equal the sum of the project's entries up to the as-of date, for every generated project and date |
| SUC-003 | A property-based test confirms the cumulative curve is non-decreasing and that its final value equals the consumed figure exactly                  |
| SUC-004 | Every threshold boundary — 74.99/75.00, 89.99/90.00, 99.99/100.00 — is asserted explicitly, including the unrounded-value rule of FR-007          |
| SUC-005 | Replaying every burn-down address as each of the three roles, assigned and unassigned, discloses no figure or person the role may not see          |
| SUC-006 | A manager identifies every project at or above 90% in one visit to the portfolio list, with no per-project navigation                             |
| SUC-007 | Deactivating a person or archiving a project changes no burn-down figure (spec 007, SUC-006)                                                      |
| SUC-008 | The burn-down issues no write of any kind, verified by asserting an unchanged store across the full scenario suite                                |
| SUC-009 | NFR-002 is met against a seeded portfolio of 500 projects and 1.5 M entries                                                                       |
| SUC-010 | No time entry saved during the scenario suite is refused, delayed or warned about on account of any budget state                                   |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Story 010 (set a budget in hours on a project)** — supplies `budgetHours`. This
  spec is not implementable before it, and has a hard expectation that story 010 refuses
  a zero or negative budget (EC-4).
- **Story 008 (projects)** — supplies `name`, `client`, `isActive` and, for FR-015 only,
  `startDate` and `endDate`. If 008 does not model those dates, FR-015 is omitted and
  everything else stands.
- **Story 009 (project assignment)** — supplies the assignment that FR-018 scopes a
  User's visibility by. **If 009 ships after this spec**, the User role sees no
  burn-down at all in the interim rather than seeing every project — the fallback is
  restrictive by design, unlike spec 001's permissive one.
- **Spec 001 (daily time entry)** — supplies every hour that is consumed and the
  *totals are derived* invariant.
- **Spec 007 (authentication and roles)** — supplies the roles of FR-017/FR-018,
  implementing its Section 2.1 matrix row *"Project burn-down: assigned projects, totals
  only / ✅ / ✅"*, and its FR-016 guarantee that a leaver's hours survive.
- **Spec 014 (chart drill-through)** — FR-020 reuses its drill, with the narrower scoping
  of EC-19. Until 014 lands, FR-020 is omitted and the figures are not openable.
- **Spec 013 (report pivot dimensions)** — EC-10 reuses its granularity rules for the
  curve's axis. Not a hard dependency; the rules can be applied independently.
- **Story 011 (correct another person's entries)** and **story 005 (edit/delete)** —
  both change consumption retroactively (EC-11, EC-12). Neither needs to know this
  feature exists, which is precisely the point of deriving everything.

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10; root namespace `pmsa`.
- Hours only — no rates, no money (Section 1.3).
- No email or messaging infrastructure exists (spec 007, Section 9.2), so every warning
  is passive and on-screen. A project can therefore reach 130% without anyone being told,
  if nobody opens the screen. This is a known and accepted limit of the slice, recorded
  in arc42 Section 11 as the first candidate for a notification story.
- **Accepted tension:** nothing warns at the point of time entry, even when booking to a
  project at 99% of its budget. Interrupting the entry row would tax spec 001's "log
  hours in seconds" goal — its NFR-001 budgets an entry at ≤ 15 keystrokes — and would
  put a project-management concern in a developer's fastest path, where they can do
  nothing about it anyway. The cost is that the person best placed to notice an overrun
  is the one not told. Recorded in arc42 Section 11.
- **Accepted tension:** the budget has no history (Section 1.3, EC-1, EC-2). A budget
  quietly raised from 400 to 600 hours turns a *Critical* project *Healthy* with no
  trace, which is exactly the manoeuvre a burn-down exists to expose. Budget history
  belongs to story 010 if it is wanted; this spec would consume it without change.
- **Accepted tension:** FR-018 makes project assignment a visibility rule, which is the
  one crack in spec 007's *roles are global* invariant. It is narrow (it only ever grants
  a User less than a Manager) and it is what spec 007's own capability matrix asks for,
  but it must be watched: generalising it would turn the flat three-role model into a
  per-project permission system the product has not asked for.
- **Accepted tension:** the projection of FR-016 uses a fixed 28 calendar-day window and
  no working calendar, so a project idle over Christmas projects a later exhaustion date
  than is realistic. A configurable window or a working calendar is more accurate and
  more to explain; the fixed window is stated in the wording so that the reader can
  discount it.

### 9.3 Architecture References

All arc42 sections are currently empty templates. The table states what this spec
expects each to say once written.

| Arc42 Section                | Relevance to This Feature                                                                                                              |
| ---------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| 03. Context & Scope          | Budgets are internal; no external system supplies a budget or is told about an overrun in this slice                                       |
| 04. Solution Strategy        | Consumption is computed, never accumulated. Every other design for a running total invites drift                                           |
| 05. Building Block View      | Adds the burn-down to the reporting building block; it shares the query side with specs 012–014 but has its own scoping rule (FR-018)      |
| 06. Runtime View             | The burn-down sequence: resolve scope and assignment, aggregate lifetime consumption in the store, derive percentage and status, build the curve |
| 08. Cross-cutting Concepts   | The derive-never-store rule; the aggregate-vs-detail visibility distinction that FR-018 and FR-020 introduce                               |
| 09. Architecture Decisions   | Needs ADRs for: lifetime-scoped consumption rather than range-scoped; deriving status from thresholds fixed in the domain; assignment as a visibility rule for this feature only; no warning in the entry path |
| 10. Quality Requirements     | NFR-002 and NFR-004 are the project's most demanding aggregate scenarios; promote them alongside spec 012's                               |
| 11. Risks & Technical Debt   | Passive-only warnings; no budget history; assignment-as-visibility as a crack in the role model; the fixed projection window                |
| 12. Glossary                 | Budget, budgeted hours, consumed hours, remaining, percentage consumed, burn-down, status bands, as-of date, projection                    |

## 10. Open Questions

| #   | Question                                                                                                              | Owner        | Status   | Resolution                                                                                                                              |
| --- | ----------------------------------------------------------------------------------------------------------------------- | ------------ | -------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Without email, how does a manager learn a project crossed 90% if they do not open the screen?                           | Product      | Open     | Not a blocker for this slice — the warning is on-screen by constraint. A notification story needs a place in the story map before release  |
| 2   | Should story 010 record budget history, so that a raised budget is visible rather than silent (EC-1, EC-2)?             | Product      | Open     | Story 010's decision. This spec consumes the current budget and would consume a history unchanged; recommended                             |
| 3   | Does story 008 model `startDate` and `endDate` on a project, which FR-015's reference line needs?                       | Product      | Open     | Not a blocker: FR-015 is a *Should* and degrades to omitting the line                                                                     |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/002/003/004/007/008/023/024,
        US-002 SC-005/006/016/017, US-003 SC-012/013/014/015, US-004 SC-009/010/011,
        US-005 SC-018/021, US-006 SC-019/020/022)
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
