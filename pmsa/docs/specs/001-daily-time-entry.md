# Feature Specification: Daily Time Entry

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
| Feature ID      | 001 (also covers story 002)                                              |
| Status          | Draft                                                                    |
| Author          | gunther bogaert                                                          |
| Created         | 2026-09-15                                                               |
| Last updated    | 2026-09-15                                                               |
| Epic / Parent   | Story map activity 1 — *Log my time*                                     |
| Arc42 reference | 05 Building Block View, 06 Runtime View, 08 Cross-cutting Concepts, 10 Quality Requirements, 12 Glossary |

> **Scope note.** This spec covers story map stories **001** (*type an entry manually
> with a begin hour, an end hour and a project*) and **002** (*see today's entries in
> order with my running daily total*). They are one screen and one domain entity:
> typing the entry and seeing the day add up are the same interaction, and splitting
> them would divide a single `TimeEntry` model across two files. The story map should
> record 002 as specified here.

> **Story map reconciliation.** Story 006 was written as *"get warned about overlapping
> entries and about days that look empty or short"*. This spec **refuses** overlapping
> entries at save time (FR-009) rather than warning about them afterwards, so story 006
> reduces to empty-or-short-day warnings. The trade is recorded in Section 9.2.

> **Terminology.** **Person**, **Role**, and entry ownership come from
> [spec 007](007-authentication-and-roles.md) and are not restated here. **Project** is
> owned by story 008; this spec consumes only its name and active flag.

### 1.1 Problem Statement

The tool being replaced makes logging hours expensive enough that developers batch the
work to Friday and reconstruct the week from memory, so the numbers that drive billing
and project burn-down are educated guesses. A developer who moves across three projects
in a day needs to record each switch in the seconds between them — if capture is not
that fast, it happens later, when it is no longer accurate.

### 1.2 Goal

A developer opens the day, types a begin time, an end time and a project, presses
Enter, and the entry is in the list with the day's total updated — without reaching for
the mouse and without leaving the screen. The list of that day's entries, in time order
with a running total, is the confirmation that the day is complete and that it adds up.

### 1.3 Non-Goals

- A timer or start/stop clock — hours are entered as begin/end times only (story map
  scope note). Fast manual entry is the whole of the capture story.
- Notes on an entry (story 003)
- The week view, daily totals across a week, and the week total (story 004)
- Editing or deleting an entry that already exists (story 005)
- Warnings about days that look empty or short (story 006) — but see the
  reconciliation note above regarding overlaps
- Creating, editing or archiving projects (story 008), and assigning people to them
  (story 009)
- Viewing or creating another person's entries (story 011)
- Any reporting, charting, burn-down or export (stories 012–016)
- Period locking, approval, or submission of a timesheet — nothing in the map calls for
  it, so any past date remains open for entry

## 2. User Stories

### US-001: Log a switch in seconds

**As a** developer,
**I want** to type a begin time, an end time and a project and press Enter,
**so that** logging a project switch costs me less than the switch itself did.

### US-002: Be told immediately when I mistype

**As a** developer,
**I want** a bad entry refused on the spot with what I typed still on screen,
**so that** I can fix it without retyping the parts that were right.

### US-003: See the day add up

**As a** developer,
**I want** the day's entries listed in time order with a running total,
**so that** I can see at a glance whether the day is complete.

### US-004: Fill in a day I missed

**As a** developer,
**I want** to change the date on the entry row and work on that day instead,
**so that** yesterday's forgotten afternoon does not have to wait for the week view.

### US-005: Trust that the day is consistent

**As a** developer,
**I want** the system to refuse an entry that overlaps one I already logged,
**so that** the same hour is never billed to two projects.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                             | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall allow a signed-in person to create a time entry from a date, a begin time, an end time and a project.                                    | Must     | US-001     |
| FR-002 | The system shall default the entry row's date to the current date on arrival.                                                                            | Must     | US-001     |
| FR-003 | The system shall accept begin and end times on a 24-hour clock with the colon optional, interpreting 1–2 digits as an hour and 3–4 digits as hour + minutes. | Must     | US-001     |
| FR-004 | The system shall offer project suggestions by case-insensitive substring match on the project name, restricted to active projects the person is assigned to. | Must     | US-001     |
| FR-005 | The system shall save the entry when Enter is pressed from any field of the entry row, requiring no pointing device at any point.                          | Must     | US-001     |
| FR-006 | The system shall, after a successful save, clear the begin time, end time and project, retain the selected date, and return focus to the begin time field. | Must     | US-001     |
| FR-007 | The system shall attribute the entry to the signed-in person, with no way to choose a different owner (spec 007, FR-005).                                  | Must     | US-001     |
| FR-008 | The system shall reject an entry whose end time is not strictly later than its begin time.                                                                | Must     | US-002     |
| FR-009 | The system shall reject an entry whose time range overlaps an existing entry of the same person on the same date, and shall name the conflicting entry.    | Must     | US-005     |
| FR-010 | The system shall accept an entry whose begin time equals the end time of an existing entry, and vice versa.                                               | Must     | US-005     |
| FR-011 | The system shall reject an entry dated later than the current date.                                                                                      | Should   | US-002     |
| FR-012 | The system shall reject an entry that is missing a date, a begin time, an end time or a project.                                                          | Must     | US-002     |
| FR-013 | The system shall, on any rejection, persist nothing and redisplay the values the person typed alongside the reason for refusal.                            | Must     | US-002     |
| FR-014 | The system shall list the signed-in person's entries for the selected date in ascending begin-time order.                                                 | Must     | US-003     |
| FR-015 | The system shall show, for each listed entry, its begin time, end time, project name and duration in decimal hours.                                        | Must     | US-003     |
| FR-016 | The system shall show the total of the listed entries' durations in decimal hours to two decimal places.                                                  | Must     | US-003     |
| FR-017 | The system shall present the newly saved entry and the recalculated total within the same interaction as the save, requiring no manual refresh.            | Must     | US-003     |
| FR-018 | The system shall show the entries and total for whichever date the entry row's date field holds, switching when that date changes.                        | Must     | US-004     |
| FR-019 | The system shall show a total of 0.00 and an invitation to log time when the selected date has no entries.                                                 | Should   | US-003     |
| FR-020 | The system shall reject an entry whose project is archived or not assigned to the person at the moment of saving.                                          | Must     | US-005     |
| FR-021 | The system shall list only the signed-in person's own entries (spec 007, FR-012).                                                                         | Must     | US-003     |

**Conventions applied where the stories left details open:** times are wall-clock local
times to the minute with no rounding; an entry may not cross midnight, so the end time
is bounded by `24:00` on the same date; durations and totals are decimal hours to two
decimal places; overlap is evaluated on half-open ranges `[begin, end)`; back-dating is
unlimited and future-dating is refused.

## 4. Acceptance Scenarios

### SC-001: Logging an entry (FR-001, FR-007)

```gherkin
Given Ann is signed in and assigned to the active project "Apollo"
  And the current date is 2026-09-15
When she enters 09:00 to 10:30 on "Apollo" and presses Enter
Then an entry of 1.50 hours on "Apollo" is recorded for Ann on 2026-09-15
  And it appears in the day's list
```

### SC-002: Shorthand time input (FR-003)

```gherkin
Given Ann is on the day screen
When she types "9" as the begin time and "1030" as the end time
Then the entry is recorded from 09:00 to 10:30
```

### SC-003: Picking a project by typing (FR-004)

```gherkin
Given Ann is assigned to the active projects "Apollo" and "Atlas"
When she types "at" in the project field
Then "Atlas" is offered and "Apollo" is not
```

### SC-004: Projects she cannot book to are not offered (FR-004)

```gherkin
Given the active project "Zeus" exists
  And Ann is not assigned to "Zeus"
When she types "ze" in the project field
Then "Zeus" is not offered
```

### SC-005: The row is ready for the next entry (FR-005, FR-006)

```gherkin
Given Ann has the date set to 2026-09-14
When she saves an entry using only the keyboard
Then the begin time, end time and project are empty
  And the date is still 2026-09-14
  And the begin time field has focus
```

### SC-006: End time must be after begin time (FR-008)

```gherkin
Given Ann is on the day screen
When she enters 14:00 to 09:00 on "Apollo"
Then the entry is not recorded
  And she is told the end time must be after the begin time
```

### SC-007: An overlapping entry is refused (FR-009)

```gherkin
Given Ann has an entry from 09:00 to 12:00 on "Apollo" for 2026-09-15
When she enters 11:00 to 13:00 on "Atlas" for 2026-09-15
Then the entry is not recorded
  And she is told it overlaps her 09:00–12:00 entry on "Apollo"
  And the day's total is still 3.00
```

### SC-008: Back-to-back entries are allowed (FR-010)

```gherkin
Given Ann has an entry from 09:00 to 10:00 on "Apollo" for 2026-09-15
When she enters 10:00 to 11:00 on "Atlas" for 2026-09-15
Then the entry is recorded
  And the day's total is 2.00
```

### SC-009: Another person's hours do not conflict (FR-009)

```gherkin
Given Bob has an entry from 09:00 to 12:00 on 2026-09-15
  And Ann is signed in
When Ann enters 09:00 to 12:00 on "Apollo" for 2026-09-15
Then the entry is recorded
```

### SC-010: A future date is refused (FR-011)

```gherkin
Given the current date is 2026-09-15
When Ann enters an entry dated 2026-09-16
Then the entry is not recorded
  And she is told hours cannot be logged for a future date
```

### SC-011: An incomplete row is refused (FR-012, FR-013)

```gherkin
Given Ann has typed 09:00 and 10:30 but has chosen no project
When she presses Enter
Then nothing is recorded
  And she is told a project is required
  And 09:00 and 10:30 are still in the row
```

### SC-012: The day is listed in time order (FR-014, FR-015)

```gherkin
Given Ann logged 14:00–16:00 on "Atlas" and then 09:00–10:30 on "Apollo",
      both for 2026-09-15
When she views 2026-09-15
Then the entries are listed 09:00–10:30 "Apollo" 1.50, then 14:00–16:00 "Atlas" 2.00
```

### SC-013: The running total (FR-016, FR-017)

```gherkin
Given Ann has 1.50 hours logged for 2026-09-15
When she saves a further entry from 13:00 to 17:20
Then the new entry is visible without refreshing the page
  And the day's total reads 5.83
```

### SC-014: Moving to another day (FR-018)

```gherkin
Given Ann is viewing 2026-09-15 with 7.50 hours logged
  And she has 4.00 hours logged for 2026-09-14
When she changes the date on the entry row to 2026-09-14
Then the list shows that day's entries
  And the total reads 4.00
  And a new entry saved now is dated 2026-09-14
```

### SC-015: An empty day (FR-019)

```gherkin
Given Ann has no entries for 2026-09-13
When she selects 2026-09-13
Then no entries are listed
  And the total reads 0.00
  And she is invited to log her first entry for that day
```

### SC-016: An archived project is refused (FR-020)

```gherkin
Given Ann selected the project "Apollo"
  And a manager archives "Apollo" before she presses Enter
When she presses Enter
Then the entry is not recorded
  And she is told "Apollo" is no longer available to book to
```

### SC-017: Only her own day is shown (FR-021)

```gherkin
Given Ann and Bob have both logged hours on 2026-09-15
When Ann views 2026-09-15
Then only Ann's entries are listed
  And the total covers only Ann's hours
```

## 5. Domain Model

### 5.1 Entities

#### TimeEntry

A contiguous block of a person's working day spent on one project. This is the atom
that every later story — the week view, corrections, reports, and project burn-down —
aggregates.

| Attribute  | Type     | Constraints                                        | Description                                                     |
| ---------- | -------- | -------------------------------------------------- | --------------------------------------------------------------- |
| id         | UUID     | PK, generated                                      |                                                                 |
| personId   | UUID     | required, references Person, immutable             | The owner; set from the session, never chosen (spec 007, FR-005) |
| projectId  | UUID     | required, references Project                       | Validated as active and assigned at save time (FR-020)           |
| date       | date     | required, not in the future                        | The calendar day the hours belong to                             |
| beginTime  | time     | required, 00:00–23:59                              | Wall-clock local time, minute precision                          |
| endTime    | time     | required, 00:01–24:00, strictly after beginTime    | `24:00` means midnight ending `date`                             |
| createdAt  | datetime | generated, immutable                               |                                                                 |
| updatedAt  | datetime | generated                                          | Reserved for story 005; unused while entries are create-only     |

`duration` is **derived**, never stored: `endTime - beginTime`, presented as decimal
hours to two decimal places. Storing it would allow it to disagree with the times.

#### Project (referenced, not owned)

Defined by story 008. This spec depends on exactly two of its attributes and treats
everything else as opaque.

| Attribute | Type    | Constraints              | Description                                            |
| --------- | ------- | ------------------------ | ------------------------------------------------------ |
| id        | UUID    | PK                       |                                                        |
| name      | string  | required                 | What the type-ahead matches on and the list displays    |
| isActive  | boolean | required                 | An archived project cannot be booked to (FR-020)        |

#### Person (referenced, not owned)

Defined by [spec 007](007-authentication-and-roles.md). Only `id` and `fullName` are
relevant here.

### 5.2 Relationships

- A **Person** owns many **TimeEntry** records (one-to-many); a TimeEntry belongs to
  exactly one Person and that ownership never changes.
- A **Project** has many **TimeEntry** records (one-to-many); a TimeEntry books to
  exactly one Project.
- A **Person** is assigned to many **Project**s and vice versa (many-to-many). *Owned
  by story 009.* This spec reads that relationship to decide what the type-ahead may
  offer and what FR-020 accepts; it never writes it.
- A **Day** is not an entity. "Ann's 2026-09-15" is the set of TimeEntries matching a
  person and a date, and the daily total is a calculation over that set — nothing about
  a day is stored.

### 5.3 Value Objects

#### TimeRange

The begin/end pair of a single entry. Meaningful only together, which is why the
`end > begin` rule belongs here rather than to `TimeEntry`.

| Attribute | Type | Constraints                                    |
| --------- | ---- | ---------------------------------------------- |
| begin     | time | required, 00:00–23:59                          |
| end       | time | required, 00:01–24:00, strictly greater than begin |

Compared as the half-open interval `[begin, end)`, so two ranges that merely touch at a
boundary do not overlap.

#### Duration

| Attribute | Type    | Constraints                                                      |
| --------- | ------- | ---------------------------------------------------------------- |
| minutes   | integer | required, 1–1440, derived from a TimeRange                        |

Presented as decimal hours, `minutes / 60`, rounded half-away-from-zero to two decimal
places. Totals are computed by summing **minutes** and converting once, never by
summing rounded hours — otherwise three 10-minute entries display as 0.50 while
totalling 0.51.

### 5.4 Domain Rules and Invariants

- **A range moves forward**: `endTime` is strictly after `beginTime`. A zero-length
  entry records nothing and is refused.
- **A day is a day**: an entry never crosses midnight. Working 22:00 to 02:00 is two
  entries on two dates. This keeps every daily total unambiguous and keeps the overlap
  check confined to a single date.
- **No hour is booked twice**: for one Person and one date, no two TimeEntry ranges
  intersect. Evaluated as `[begin, end)`, so 09:00–10:00 and 10:00–11:00 coexist and
  10:59–11:30 does not.
- **Overlap is personal**: the rule is scoped per Person. Two people booking the same
  hour, even to the same project, is normal and never refused.
- **A day cannot exceed 24 hours**: a consequence of the two rules above rather than a
  separate check — non-overlapping ranges bounded by one calendar date can total at
  most 24.00. Any implementation that can produce a daily total above 24.00 has
  violated one of them.
- **Hours belong to the past**: `date` is never later than the current date. Hours that
  have not been worked cannot be logged.
- **Immutable ownership**: the owning Person is set at creation from the session and is
  never reassigned (spec 007).
- **Bookability is checked at the moment of saving**: the project must be active and
  assigned to the person when the entry is written, not merely when it was offered in
  the type-ahead. The suggestion list is a convenience; FR-020 is the rule.
- **Nothing is half-saved**: an entry that fails any rule leaves no trace — no partial
  record, no consumed identifier, no change to any total.
- **Totals are derived**: no daily or weekly total is stored anywhere. Every total is
  computed from the entries it covers, so no total can drift from its entries.
- **History is honest**: a saved entry keeps the times it was given. Nothing rounds,
  snaps or normalises a time after the fact.

## 6. Non-Functional Requirements

| ID      | Category      | Requirement                                                                                                                                   |
| ------- | ------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Usability     | A complete entry is possible with no pointing device: begin, Tab, end, Tab, a project prefix, Enter. A typical entry costs ≤ 15 keystrokes including Enter. |
| NFR-002 | Performance   | Saving an entry and presenting the updated list and total completes in < 300 ms at p95, so the next entry can be typed without waiting.         |
| NFR-003 | Performance   | The day screen renders in < 500 ms at p95 for a day holding up to 20 entries.                                                                  |
| NFR-004 | Performance   | Project suggestions appear within 100 ms of a keystroke for a catalogue of up to 500 projects.                                                 |
| NFR-005 | Reliability   | The overlap check (FR-009) and the write are atomic with respect to concurrent saves by the same person, so no pair of overlapping entries can be created by two simultaneous requests. |
| NFR-006 | Scalability   | Retrieving one person's entries for one date stays within NFR-003 at 200 persons × 10 entries per working day × 3 years (~1.5 M entries), which requires the store to resolve (person, date) without scanning. |
| NFR-007 | Correctness   | A daily total always equals the sum of the durations listed beneath it, to the displayed precision, with no accumulated rounding error.         |
| NFR-008 | Security      | A person can create and read only their own entries; the owner is taken from the session and is never accepted from the request (spec 007, FR-005 and FR-012). |
| NFR-009 | Accessibility | The entry row is operable and announced by a screen reader: every field is labelled, refusals are announced, and the running total is announced when it changes. |

Project-wide quality requirements (transport security, logging, error presentation)
belong in arc42 Section 10 and are referenced, not restated here.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                              | Expected Behavior                                                                                                        |
| ----- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| EC-1  | Enter pressed twice in quick succession on the same input                             | One entry only. The second request is refused by the overlap rule, which makes exact duplicates impossible by construction |
| EC-2  | A begin and end time that are equal (09:00–09:00)                                     | Refused as a zero-length entry; the message names the rule, not the field                                                  |
| EC-3  | `"25"`, `"9:75"`, `"abc"` typed as a time                                             | Refused with the accepted formats stated; nothing is guessed or snapped to a nearby valid time                             |
| EC-4  | `24:00` entered as the begin time                                                     | Refused — `24:00` is an end-of-day marker and is valid only as an end time                                                 |
| EC-5  | An entry from 23:00 to 24:00                                                          | Accepted as 1.00 hour; the day's total may reach exactly 24.00                                                            |
| EC-6  | A new entry entirely inside an existing one, or entirely containing one               | Both refused by FR-009; containment is overlap                                                                            |
| EC-7  | A new entry identical to an existing one                                              | Refused as an overlap, naming the existing entry — this is the duplicate-submission guard of EC-1                          |
| EC-8  | The date is changed while the row holds partly typed times                            | The list and total switch to the new date; the typed begin, end and project are preserved                                  |
| EC-9  | A project is archived, or the person is unassigned from it, between page load and save | Refused (FR-020) with the project named; the suggestion list is refreshed                                                  |
| EC-10 | The person is assigned to no active projects at all                                   | The row cannot be completed; they are told to ask a manager for a project assignment (story 009) rather than shown an empty dropdown |
| EC-11 | A non-existent date such as 2026-02-30 is typed                                       | Refused as an invalid date; no entry and no change of selected day                                                         |
| EC-12 | A date far in the past, e.g. 2019-01-02                                               | Accepted — no period is locked. If locking is ever introduced it becomes a new story, not a change here                     |
| EC-13 | Two devices save overlapping entries for the same person at the same moment           | Serialised (NFR-005); the second is refused with the overlap message. The invariant holds under concurrency                 |
| EC-14 | A time that does not exist locally because of a daylight-saving jump, e.g. 02:30 on a spring-forward date | Accepted. Times are stored as wall-clock values against a date with no timezone conversion, so DST is deliberately not validated — recorded as a known limitation in arc42 Section 11 |
| EC-15 | An entry spanning most of a day, e.g. 06:00–23:00                                     | Accepted. Implausibly long days are story 006's concern, not a validation failure here                                     |
| EC-16 | A very long project name in the list                                                  | Truncated for layout with the full name still obtainable; the name is never altered in storage                             |
| EC-17 | The session expires while the row is being filled in                                  | Spec 007, EC-12 applies: redirected to sign-in, returned to this page afterwards, typed input lost                          |
| EC-18 | The selected date is 2026-09-15 and midnight passes while the screen is open          | The selected date does not move on its own. "Today" is resolved when the row is first shown and when a save is validated (FR-011) |

## 8. Success Criteria

<!-- Success criteria are prefixed SUC- to avoid colliding with the SC- acceptance
     scenario identifiers in Section 4. -->

| ID      | Criterion                                                                                                            |
| ------- | -------------------------------------------------------------------------------------------------------------------- |
| SUC-001 | All acceptance scenarios SC-001 to SC-017 pass automatically in CI                                                    |
| SUC-002 | A developer logs three entries for one day using the keyboard only, from arrival to third total, in under 30 seconds  |
| SUC-003 | A property-based test over generated entry sequences produces no pair of overlapping entries for one person and date  |
| SUC-004 | For every day in a generated data set, the displayed total equals the sum of the displayed durations, and never exceeds 24.00 |
| SUC-005 | Every requirement that refuses an entry (FR-008 to FR-013, FR-020) is shown to leave the store unchanged              |
| SUC-006 | No request can create an entry owned by anyone other than the signed-in person, verified by replaying a save with a substituted owner |
| SUC-007 | A person can fill in a missed day from the same screen without navigating away from it                                |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Spec 007 (authentication and roles)** — supplies the signed-in Person, entry
  ownership (FR-005) and own-data scoping (FR-012). This spec is not implementable
  before it.
- **Story 008 (projects)** — supplies the Project entity. Only `name` and `isActive`
  are consumed.
- **Story 009 (project assignment)** — filters what FR-004 offers and what FR-020
  accepts. **If 009 ships after this spec**, FR-004 and FR-020 fall back to *all active
  projects* in the interim, and narrow to assigned projects when 009 lands. That change
  is visible to users and should be expected, not treated as a regression.
- **Story 003 (notes)** adds an optional attribute to `TimeEntry`; nothing here should
  assume the entity is closed.
- **Story 005 (edit and delete)** must apply the same overlap rule (FR-009) to an
  edited entry, excluding the entry being edited from its own conflict check.
- **Story 004 (week view)** and **stories 012–016 (reporting)** aggregate this entity
  and depend on the *totals are derived* invariant.
- **Story 006** is reduced by this spec to empty-or-short-day warnings.

### 9.2 Constraints

- ASP.NET Core Razor Pages on .NET 10; root namespace `pmsa`.
- No timer or start/stop capture, by product decision recorded in the story map.
- Wall-clock local times with no timezone handling — the product serves one
  organisation in one timezone. Introducing a second would invalidate EC-14 and the
  storage model behind it.
- **Accepted tension:** refusing overlaps (FR-009) puts a hard stop in the middle of the
  fastest path, which taxes the "log hours in seconds" goal, and it moves work out of
  story 006. It was chosen knowingly because a week that silently double-books an hour
  defeats the reporting and burn-down half of the product goal. Recorded in arc42
  Section 11 as a friction risk to revisit if refusals turn out to be common.
- **Accepted tension:** the entry row's date field doubles as the day selector (FR-018).
  One control for two jobs is economical and keeps the screen keyboard-only, but it
  means changing the date silently changes what the list below means. NFR-009's
  announcement requirement exists partly to mitigate this.

### 9.3 Architecture References

All arc42 sections are currently empty templates. The table states what this spec
expects each to say once written.

| Arc42 Section                | Relevance to This Feature                                                                                                  |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| 03. Context & Scope          | Time entry is wholly internal; no external system supplies or consumes hours in this slice                                  |
| 05. Building Block View      | Introduces the time-entry building block and the `TimeEntry` store that every later activity reads                          |
| 06. Runtime View             | The save sequence: validate, check overlap and bookability atomically, write, return the recalculated day                   |
| 08. Cross-cutting Concepts   | Validation and refusal presentation; the derived-totals rule that stories 004 and 012–016 must not break                    |
| 09. Architecture Decisions   | Needs ADRs for: storing times as wall-clock values with no timezone; refusing rather than flagging overlaps; deriving durations and totals rather than storing them |
| 10. Quality Requirements     | NFR-002, NFR-003 and NFR-007 are the first concrete quality scenarios the project has; promote the response-time targets here |
| 11. Risks & Technical Debt   | Overlap refusal versus the speed goal; no DST validation (EC-14); the single-timezone assumption                            |
| 12. Glossary                 | TimeEntry, entry, day, daily total, duration, decimal hours, overlap, bookable project                                      |

## 10. Open Questions

| #   | Question                                                                                                | Owner        | Status   | Resolution                                                                                             |
| --- | ------------------------------------------------------------------------------------------------------- | ------------ | -------- | ------------------------------------------------------------------------------------------------------ |
| 1   | Does story 009 (project assignment) ship before this feature?                                           | Product      | Open     | Sequencing decision. The interim fallback is stated in Section 9.1, so implementation is not blocked    |
| 2   | Do reports (012–016) need a different rounding rule from this feature's two-decimal hours — e.g. billing increments? | Product | Deferred | Not a blocker: this spec stores minutes, so any presentation rounding remains available downstream       |
| 3   | Should story 006's empty/short-day warnings be re-scoped now that overlaps are refused here?             | Product      | Resolved | Yes. Story 006 was narrowed to empty and short days on 2026-09-15; the reasoning is recorded in the story map's scope notes |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios (US-001 SC-001/002/005, US-002
        SC-006/010/011, US-003 SC-012/013/015/017, US-004 SC-014, US-005 SC-007/008/009/016)
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
