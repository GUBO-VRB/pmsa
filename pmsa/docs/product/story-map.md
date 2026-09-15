# User Story Map: pmsa

## Goal
Replace the old-school hour-entry tool with something developers can log to in seconds while switching between projects, and that gives everyone their hours back as insight — per user, per project, per timespan, with project budget burn-down — instead of a single flat report.

## Primary User
A developer who works on several projects in the same day and logs hours as they switch onto each one. They want entry to be fast and forgettable, and they want to trust that the week adds up. In role terms they are a **User**. A **Manager** additionally manages projects and people's entries and watches how project budgets are being consumed; an **Admin** additionally administers the accounts themselves.

> **Vocabulary.** This map originally spoke of *regular* and *elevated* users. Spec 007 refined that into three roles — **User**, **Manager**, **Admin** — held by a **Person** (the account entity; "User" is taken by the role). The map below uses that vocabulary; see [arc42 Section 12](../architecture/12-glossary.md) for the definitions.

## Story Map

### Log my time
| #   | Story                                                                        |
| --- | ---------------------------------------------------------------------------- |
| 001 | Type an entry manually with a begin hour, an end hour and a project — *[spec 001](../specs/001-daily-time-entry.md)* |
| 002 | See today's entries in order with my running daily total — *covered by [spec 001](../specs/001-daily-time-entry.md); same screen, same entity* |
| 003 | Add a short note to an entry so I remember what I actually did               |

### Review & fix my week
| #   | Story                                                                        |
| --- | ---------------------------------------------------------------------------- |
| 004 | See my whole week laid out day by day, with daily totals and a week total     |
| 005 | Edit or delete an entry I got wrong                                          |
| 006 | Get warned about days that look empty or short                               |

### Manage projects, people & access
| #   | Story                                                                                  |
| --- | -------------------------------------------------------------------------------------- |
| 007 | Sign in, with User, Manager and Admin seeing different capabilities — *[spec 007](../specs/007-authentication-and-roles.md)* ✅ implemented |
| 008 | Create and edit a project: name, client, colour, active or archived                     |
| 009 | Assign which people can book hours to which project                                     |
| 010 | Set a budget in hours on a project so consumption can be tracked against it             |
| 011 | As a Manager, correct or fill in another person's entries                               |

### Report & visualize
| #   | Story                                                                                       |
| --- | ------------------------------------------------------------------------------------------- |
| 012 | Pick a date range and see hours broken down per project as a chart — *[spec 012](../specs/012-date-range-project-report.md)* ✅ implemented |
| 013 | Switch the angle of the same data: per person, per project, or per day — *[spec 013](../specs/013-report-pivot-dimensions.md)* |
| 014 | Drill into a slice of a chart to see the underlying entries behind it — *[spec 014](../specs/014-chart-drill-through.md)* |
| 015 | See a project burn-down: budgeted hours versus logged hours, percentage consumed, with a warning as it nears the budget — *[spec 015](../specs/015-project-burn-down.md)* |
| 016 | Export the filtered result to CSV/Excel                                                     |

## Notes on scope

- **No timer.** Hours are entered manually as begin/end times only — there is no start/stop clock. Fast manual entry (story 001) is the whole of the capture story.
- The point of difference versus the tool being replaced is **follow-up**: activities 2 and 4 are where the value lives, not activity 1.
- Reporting was explicitly asked to be multi-angle (user / project / timespan), so story 013 is a first-class story rather than a variation of 012.
- Project budget + burn-down (010, 015) was deliberately pulled in as the ambitious piece — it is the story that makes the app worth opening when you are not logging hours.
- **Overlaps are refused, not warned about.** Story 006 originally read "get warned about overlapping entries *and* about days that look empty or short". Spec 001 decided to refuse an overlapping entry outright at save time, so that half of 006 moved into 001 and story 006 now covers empty and short days only. The trade — a hard stop in the fastest path, in exchange for a week that can never double-book an hour — is recorded in spec 001, Section 9.2.
- Story 007 was specified ahead of activity 1 because every other story is scoped to a person ("*my* entries, *my* week"), so entry ownership has to exist before 001 can be built. It is now implemented; the capability matrix it established is in [arc42 Section 8](../architecture/08-cross-cutting-concepts.md) and every story below is bound by it.
- **Who each story is for.** Activities 1 and 2 (001–006) are User-level: your own entries, your own week. Stories 008–011 and the cross-person half of 012–016 are Manager-level. Only account administration is Admin-level, and it has no story of its own — it arrived inside 007.
- **Story 009 is not an authorization story.** Project assignment decides which projects a person may *book hours to*. It does not grant capabilities — roles are global. Reading 009 as a permission mechanism would put two competing answers in the system for the same question.
- **Password recovery has no home yet.** With no email infrastructure, a forgotten password or a lockout is cleared by an Admin in person (spec 007, open question 1). That is tolerable for an internal team but needs a story before release; it is tracked as a risk in [arc42 Section 11](../architecture/11-risks-and-technical-debt.md).
- **Reporting is one screen, not four.** Specs 012, 013 and 014 layer on each other: 012 owns the report screen (range, filters, scoping, rounding, chart + table), 013 adds the grouping dimension, 014 adds drill-through. Spec 015 is deliberately a *separate* screen, because a budget is a lifetime envelope and a range-scoped percentage would be meaningless.
- **Story 013's "per day" is specified as a *Time* dimension** that buckets by day, ISO week or month depending on the range length (spec 013, FR-011), because 366 daily bars is not a chart. The literal per-day view stays reachable via an explicit override.
- **Story 015's "warning" is passive.** With no email infrastructure (spec 007), a budget warning is a status on a screen. Nothing warns in the time-entry path either — that would tax story 001's speed goal for something a developer cannot act on. Both trades are recorded in spec 015, Section 9.2.
