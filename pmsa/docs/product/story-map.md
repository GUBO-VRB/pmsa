# User Story Map: pmsa

## Goal
Replace the old-school hour-entry tool with something developers can log to in seconds while switching between projects, and that gives everyone their hours back as insight — per user, per project, per timespan, with project budget burn-down — instead of a single flat report.

## Primary User
A developer who works on several projects in the same day and logs hours as they switch onto each one. They want entry to be fast and forgettable, and they want to trust that the week adds up. A secondary, "elevated" user manages projects and people, corrects entries, and watches how project budgets are being consumed.

## Story Map

### Log my time
| #   | Story                                                                        |
| --- | ---------------------------------------------------------------------------- |
| 001 | Type an entry manually with a begin hour, an end hour and a project          |
| 002 | See today's entries in order with my running daily total                     |
| 003 | Add a short note to an entry so I remember what I actually did               |

### Review & fix my week
| #   | Story                                                                        |
| --- | ---------------------------------------------------------------------------- |
| 004 | See my whole week laid out day by day, with daily totals and a week total     |
| 005 | Edit or delete an entry I got wrong                                          |
| 006 | Get warned about overlapping entries and about days that look empty or short |

### Manage projects, people & access
| #   | Story                                                                                  |
| --- | -------------------------------------------------------------------------------------- |
| 007 | Sign in, with regular users and elevated users seeing different capabilities            |
| 008 | Create and edit a project: name, client, colour, active or archived                     |
| 009 | Assign which people can book hours to which project                                     |
| 010 | Set a budget in hours on a project so consumption can be tracked against it             |
| 011 | As an elevated user, correct or fill in another person's entries                        |

### Report & visualize
| #   | Story                                                                                       |
| --- | ------------------------------------------------------------------------------------------- |
| 012 | Pick a date range and see hours broken down per project as a chart                          |
| 013 | Switch the angle of the same data: per person, per project, or per day                      |
| 014 | Drill into a slice of a chart to see the underlying entries behind it                       |
| 015 | See a project burn-down: budgeted hours versus logged hours, percentage consumed, with a warning as it nears the budget |
| 016 | Export the filtered result to CSV/Excel                                                     |

## Notes on scope

- **No timer.** Hours are entered manually as begin/end times only — there is no start/stop clock. Fast manual entry (story 001) is the whole of the capture story.
- The point of difference versus the tool being replaced is **follow-up**: activities 2 and 4 are where the value lives, not activity 1.
- Reporting was explicitly asked to be multi-angle (user / project / timespan), so story 013 is a first-class story rather than a variation of 012.
- Project budget + burn-down (010, 015) was deliberately pulled in as the ambitious piece — it is the story that makes the app worth opening when you are not logging hours.
