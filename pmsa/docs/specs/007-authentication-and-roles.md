# Feature Specification: Authentication and Roles

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

| Field           | Value                                                            |
| --------------- | ---------------------------------------------------------------- |
| Feature ID      | 007                                                              |
| Status          | Implemented                                                      |
| Author          | gunther bogaert                                                  |
| Created         | 2026-09-15                                                       |
| Last updated    | 2026-09-15                                                       |
| Epic / Parent   | Story map activity 3 — *Manage projects, people & access*        |
| Arc42 reference | 08 Cross-cutting Concepts, 09 Architecture Decisions, 10 Quality Requirements, 12 Glossary |

> **Terminology note.** The story map (story 007) speaks of *regular* and *elevated*
> users. This spec refines "elevated" into two roles — **Manager** and **Admin** — and
> uses **Person** for the account entity, because "User" is already taken by the role
> of the same name. The story map should be reconciled to this vocabulary.

### 1.1 Problem Statement

Every other story in the map is scoped to a person: *my* entries, *my* week, *my*
hours. Without authentication the system has no notion of who "my" refers to, and
without differentiated capabilities nothing prevents any user from editing a
colleague's timesheet or changing a project's budget. The tool being replaced is
trusted with how people are paid and how projects are billed, so unattributed or
unprotected time data makes the replacement unusable regardless of how good the entry
and reporting experience is.

### 1.2 Goal

Every request to the system is made by a known, active person, and the capabilities
that person sees are exactly those their role permits. A developer signs in and
reaches only their own hours; a manager additionally reaches projects, budgets, and
other people's entries; an admin additionally manages accounts and roles. People who
leave keep their history intact but lose access.

### 1.3 Non-Goals

- Self-service registration — accounts are created by an Admin
- Password recovery / "forgot password" flows, and any email or SMTP dependency
- Single sign-on, external identity providers, or multi-factor authentication
- Multi-tenancy — one organisation; *client* on a project (story 008) is a label, not
  a data boundary
- Per-project roles — *which* projects you may book to is story 009, not a role
- Audit logging of sign-in activity or of administrative changes
- Deleting a person (see the *Deactivate, never delete* invariant)

## 2. User Stories

### US-001: Sign in

**As a** developer,
**I want** to sign in with my own credentials,
**so that** the hours I log are attributed to me.

### US-002: Sign out

**As a** developer,
**I want** to sign out,
**so that** nobody can log hours as me on a machine I have left.

### US-003: An uncluttered, safe interface

**As a** developer,
**I want** capabilities I am not permitted to use to be absent from my screen,
**so that** logging hours stays a two-second job.

### US-004: Manage as a manager

**As a** manager,
**I want** my role to grant me projects, budgets and other people's entries,
**so that** I can keep the data correct without needing a separate account.

### US-005: Onboard a colleague

**As an** admin,
**I want** to create an account for a new colleague,
**so that** they can start logging hours on their first day.

### US-006: Offboard a leaver

**As an** admin,
**I want** to deactivate someone who has left,
**so that** they lose access while their logged hours stay in the reports.

### US-007: Change someone's role

**As an** admin,
**I want** to change someone's role,
**so that** responsibilities can shift without recreating their account and losing
their history.

### US-008: Change my own password

**As a** signed-in person,
**I want** to change my own password,
**so that** I am not dependent on an admin for a routine change.

### 2.1 Role Capability Matrix

| Capability                                       |      User      | Manager | Admin |
| ------------------------------------------------ | :------------: | :-----: | :---: |
| Log, view, edit own entries (stories 001–006)     |       ✅       |   ✅    |  ✅   |
| View or edit any person's entries (story 011)     |       —        |   ✅    |  ✅   |
| Create and edit projects and budgets (008, 010)   |       —        |   ✅    |  ✅   |
| Assign people to projects (story 009)             |       —        |   ✅    |  ✅   |
| Reports per person (stories 012–016)              | own hours only |   ✅    |  ✅   |
| Project burn-down (story 015)                     | assigned projects, totals only | ✅ | ✅ |
| Create accounts, set roles, deactivate people     |       —        |    —    |  ✅   |

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                     | Priority | User Story |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall authenticate a person by email address and password and establish a session on success.                                          | Must     | US-001     |
| FR-002 | The system shall reject invalid credentials with a message that does not reveal whether the email address exists.                                 | Must     | US-001     |
| FR-003 | The system shall refuse sign-in to a deactivated account, giving the same non-specific message as FR-002.                                         | Must     | US-006     |
| FR-004 | The system shall require an authenticated session for every page except the sign-in page, redirecting unauthenticated visitors to sign-in.        | Must     | US-001     |
| FR-005 | The system shall attribute every time entry created in a session to the signed-in person, with no way to choose a different owner (except FR-011). | Must     | US-001     |
| FR-006 | The system shall end the session on sign out and return the person to the sign-in page, such that navigating back does not restore access.        | Must     | US-002     |
| FR-007 | The system shall expire a session 12 hours after sign-in and on browser close, requiring re-authentication.                                       | Must     | US-002     |
| FR-008 | The system shall temporarily lock an account for 15 minutes after 5 consecutive failed sign-in attempts.                                          | Should   | US-001     |
| FR-009 | The system shall omit from the interface every navigation item and action the signed-in person's role does not permit.                            | Must     | US-003     |
| FR-010 | The system shall refuse any directly-addressed request for a capability above the person's role, independently of whether the interface offered it.| Must     | US-003     |
| FR-011 | The system shall grant the Manager and Admin roles read and write access to all projects, budgets, project assignments, and all people's entries.  | Must     | US-004     |
| FR-012 | The system shall restrict the User role to their own time entries, and to reports covering their own hours plus aggregate consumption of projects they are assigned to. | Must | US-003 |
| FR-013 | The system shall allow an Admin to create a person with full name, unique email address, role, and an initial password.                            | Must     | US-005     |
| FR-014 | The system shall require a person to set a new password at their first sign-in after an Admin set their password.                                  | Should   | US-005     |
| FR-015 | The system shall allow an Admin to deactivate and reactivate a person.                                                                            | Must     | US-006     |
| FR-016 | The system shall retain a deactivated person's time entries and continue to include them in all reports and budget burn-down.                      | Must     | US-006     |
| FR-017 | The system shall allow an Admin to change any person's role, taking effect at that person's next request.                                          | Must     | US-007     |
| FR-018 | The system shall prevent any action that would leave zero active Admin accounts.                                                                  | Must     | US-007     |
| FR-019 | The system shall allow a signed-in person to change their own password by supplying their current password.                                        | Should   | US-008     |
| FR-020 | The system shall provide a seeded Admin account on first deployment, subject to FR-014.                                                            | Must     | US-005     |
| FR-021 | The system shall terminate an active session at the next request if that person has been deactivated.                                              | Must     | US-006     |

**Conventions applied where the story left details open:** the sign-in identifier is
the email address; the session cookie is non-persistent with a 12-hour absolute
expiry; the minimum password length is 12 characters with no composition rules.

## 4. Acceptance Scenarios

### SC-001: Successful sign-in (FR-001)

```gherkin
Given an active person "ann@acme.example" with a known password
When she signs in with that email and password
Then she is signed in and lands on today's entries
  And the interface identifies her by name
```

### SC-002: Wrong password (FR-002)

```gherkin
Given an active person "ann@acme.example"
When she signs in with the correct email and a wrong password
Then she is not signed in
  And she is told "Email address or password is incorrect"
```

### SC-003: Unknown email is indistinguishable from a wrong password (FR-002)

```gherkin
Given no person exists with email "nobody@acme.example"
When someone signs in with that email and any password
Then they are not signed in
  And they are told "Email address or password is incorrect"
```

### SC-004: Deactivated account cannot sign in (FR-003)

```gherkin
Given a deactivated person "bob@acme.example" with a known password
When he signs in with the correct email and password
Then he is not signed in
  And he is told "Email address or password is incorrect"
```

### SC-005: Unauthenticated visitor is redirected (FR-004)

```gherkin
Given nobody is signed in
When a visitor opens the weekly overview
Then they are sent to the sign-in page
  And after signing in successfully they arrive at the weekly overview
```

### SC-006: Entries are attributed to the signed-in person (FR-005)

```gherkin
Given Ann is signed in
When she records an entry from 09:00 to 10:30 on project "Apollo"
Then the entry is owned by Ann
  And it appears in Ann's day, and in no other person's day
```

### SC-007: Sign out (FR-006)

```gherkin
Given Ann is signed in
When she signs out and then navigates back to the weekly overview
Then she is sent to the sign-in page
```

### SC-008: Session expires (FR-007)

```gherkin
Given Ann signed in 12 hours ago and her session has not been renewed
When she opens today's entries
Then she is sent to the sign-in page
```

### SC-009: Repeated failures lock the account temporarily (FR-008)

```gherkin
Given an active person "ann@acme.example"
When 5 consecutive sign-in attempts with a wrong password are made
Then the 6th attempt is refused even with the correct password
  And 15 minutes later the correct password signs her in
```

### SC-010: A User is not shown capabilities above their role (FR-009)

```gherkin
Given Ann is signed in with the User role
When she views any page
Then no project management, people management, or account administration
     navigation is present
```

### SC-011: A User is refused a capability reached directly (FR-010)

```gherkin
Given Ann is signed in with the User role
When she requests the project budget screen by its address directly
Then the request is refused as not permitted
  And no budget data is disclosed
```

### SC-012: A Manager corrects someone else's entry (FR-011)

```gherkin
Given Mo is signed in with the Manager role
  And Ann has an entry from 09:00 to 10:30 on project "Apollo"
When Mo changes the entry's end time to 11:00
Then the entry still belongs to Ann
  And Ann's daily total reflects 2 hours
```

### SC-013: A User's reports are scoped to their own hours (FR-012)

```gherkin
Given Ann is signed in with the User role
  And Ann and Bob have both logged hours on project "Apollo"
When she reports on "Apollo" over this month
Then she sees her own hours and the project's total consumption against budget
  And she does not see Bob's hours attributed to Bob
```

### SC-014: An Admin creates an account (FR-013)

```gherkin
Given Dee is signed in with the Admin role
When she creates a person "cy@acme.example" with the User role and an
     initial password
Then the account exists, is active, and can sign in with that password
```

### SC-015: Email addresses are unique (FR-013)

```gherkin
Given a person already exists with email "ann@acme.example"
When an Admin creates another person with that same email address
Then the account is not created
  And the Admin is told the email address is already in use
```

### SC-016: First sign-in forces a password change (FR-014)

```gherkin
Given an Admin has just set an initial password for "cy@acme.example"
When Cy signs in with that password
Then he is required to choose a new password before reaching any other page
  And his subsequent sign-ins do not ask again
```

### SC-017: A leaver is deactivated and their history survives (FR-015, FR-016)

```gherkin
Given Bob has 40 logged hours on project "Apollo"
When an Admin deactivates Bob
Then Bob can no longer sign in
  And "Apollo" still shows those 40 hours consumed against its budget
```

### SC-018: A role change takes effect on the next request (FR-017)

```gherkin
Given Mo is signed in with the Manager role and is viewing a project
When an Admin changes Mo's role to User
  And Mo refreshes the page
Then the request is refused as not permitted
```

### SC-019: The last Admin cannot be removed (FR-018)

```gherkin
Given Dee is the only active Admin
When she attempts to deactivate herself or change her own role to User
Then the change is refused
  And she is told the system must retain at least one active Admin
```

### SC-020: Changing your own password requires the current one (FR-019)

```gherkin
Given Ann is signed in
When she submits a new password together with an incorrect current password
Then her password is unchanged
  And she is told the current password is incorrect
```

### SC-021: Deactivation ends a live session (FR-021)

```gherkin
Given Bob is signed in with the User role
When an Admin deactivates Bob
  And Bob performs any action
Then he is sent to the sign-in page
  And he cannot sign in again
```

## 5. Domain Model

### 5.1 Entities

#### Person

A member of the organisation who can sign in and to whom time entries belong. Named
*Person* rather than *User* because **User** is one of the three roles.

| Attribute            | Type     | Constraints                                       | Description                                                       |
| -------------------- | -------- | ------------------------------------------------- | ----------------------------------------------------------------- |
| id                   | UUID     | PK, generated                                     | Stable identity; referenced by every time entry                   |
| fullName             | string   | required, 1–200 chars                             | Display name shown in the interface and in reports                |
| email                | string   | required, unique (case-insensitive), max 254      | The sign-in identifier                                            |
| passwordHash         | string   | required                                          | One-way hash with per-person salt; never exposed by any interface |
| role                 | enum     | required, [User, Manager, Admin], default User    | Grants capabilities globally, not per project                     |
| isActive             | boolean  | required, default true                            | False means the person cannot sign in                             |
| mustChangePassword   | boolean  | required, default true on admin-set passwords     | Blocks all other capabilities until resolved                      |
| failedAttemptCount   | integer  | required, >= 0, default 0                         | Reset to 0 on any successful sign-in                              |
| lockedUntil          | datetime | nullable                                          | While in the future, sign-in is refused regardless of password    |
| createdAt            | datetime | generated, immutable                              |                                                                   |
| deactivatedAt        | datetime | nullable                                          | Set when isActive becomes false, cleared on reactivation          |

#### Session

The conceptual record of a signed-in person. It may be realised entirely as a signed
cookie rather than stored data — the model below states what a session *means*, not
where it lives.

| Attribute  | Type     | Constraints                         | Description                                     |
| ---------- | -------- | ----------------------------------- | ----------------------------------------------- |
| personId   | UUID     | required, references Person         | Whose session this is                           |
| signedInAt | datetime | required, immutable                 |                                                 |
| expiresAt  | datetime | required, = signedInAt + 12 hours   | Absolute expiry; not extended by activity       |

Note that `role` is deliberately **not** a session attribute — it is read from the
Person on every request so that FR-017 and FR-021 take effect immediately.

### 5.2 Relationships

- A **Person** owns many **TimeEntry** records (one-to-many); a **TimeEntry** belongs
  to exactly one Person and that ownership never changes. *TimeEntry is defined by
  spec 001; this spec only establishes the owning reference.*
- A **Person** may have several concurrent **Session**s (one per browser/device); each
  Session belongs to exactly one Person.
- A **Person** is assigned to many **Project**s and a Project to many People
  (many-to-many). *That relationship is owned by story 009 and governs which projects
  a person may book to — it does not confer any role or management capability.*

### 5.3 Value Objects

#### EmailAddress

| Attribute  | Type   | Constraints                                                        |
| ---------- | ------ | ------------------------------------------------------------------ |
| value      | string | required, max 254, syntactically valid, normalised to lower case and trimmed before comparison or storage |

#### Password

Only ever exists in memory during sign-in or a password change; it is stored solely
as its hash.

| Attribute  | Type   | Constraints                                                        |
| ---------- | ------ | ------------------------------------------------------------------ |
| value      | string | required, minimum 12 characters, no composition rules, no maximum below 128 |

### 5.4 Domain Rules and Invariants

- **Unique identity**: no two Persons share an email address, compared
  case-insensitively, regardless of whether either is active. A leaver's address is
  not reusable, because rebinding it would silently reattribute their history.
- **At least one active Admin**: the system must always contain at least one Person
  with role `Admin` and `isActive = true`. Any deactivation or role change that would
  violate this is refused (FR-018), including when two Admins act concurrently.
- **Deactivate, never delete**: a Person is never removed. Deletion would orphan or
  destroy time entries and retroactively change project burn-down (story 015).
- **Immutable ownership**: a time entry's owning Person is set at creation and never
  reassigned. A Manager editing someone's entry changes its content, never its owner.
- **Passwords are irreversible**: no stored, logged, or transmitted form of a password
  allows recovery of the original. Nothing in the system can display a password.
- **Roles are global**: a role grants the same capabilities everywhere. There is no
  combination of project assignment and role that produces a different capability set.
- **No session outlives its account**: a deactivated Person has no valid session; the
  check happens per request, not per sign-in.
- **Server-side enforcement**: omitting an action from the interface (FR-009) is a
  usability measure. Authorization is the refusal at the request (FR-010), and every
  capability must be enforceable with no interface involved at all.
- **Forced change is blocking**: while `mustChangePassword` is true, the Person can
  reach nothing but the password-change screen and sign-out.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                                                  |
| ------- | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Security     | Passwords are stored with an adaptive, memory-hard hash (Argon2id, or PBKDF2 at current OWASP iteration guidance) with a unique per-person salt. |
| NFR-002 | Security     | All authenticated traffic is HTTPS-only; the session cookie is `HttpOnly`, `Secure`, `SameSite=Lax`.                                          |
| NFR-003 | Security     | Sign-in performs equivalent work for an unknown email as for a wrong password, so that response time does not disclose whether an account exists. |
| NFR-004 | Security     | The session identifier is regenerated on sign-in and on password change (session-fixation resistance).                                        |
| NFR-005 | Security     | Authenticated pages are served with `Cache-Control: no-store`, so the back button after sign-out cannot reveal data.                          |
| NFR-006 | Performance  | Sign-in completes in < 1 s at p95 (deliberately slow hashing accounts for most of this); the per-request authorization check adds < 5 ms at p95. |
| NFR-007 | Reliability  | Seeding the Admin account is idempotent: redeploying never resets an existing Admin's password or reactivates a deactivated one.              |
| NFR-008 | Usability    | After a session expires, signing in returns the person to the page they originally requested.                                                  |
| NFR-009 | Scalability  | Designed for up to 200 Persons; no pagination or search is required on the people list below that number.                                      |

Project-wide quality requirements (transport security, logging, error handling) belong
in arc42 Section 10 and are referenced, not restated here.

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                    | Expected Behavior                                                                                  |
| ----- | --------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| EC-1  | Sign-in submitted with an empty email or password                           | Field-level validation message; no authentication attempt, no lockout counter increment             |
| EC-2  | Email entered as " Ann@Acme.Example " with padding and mixed case            | Normalised (trimmed, lower-cased) and signs in successfully                                         |
| EC-3  | A Person is deactivated while they are mid-way through submitting a form     | The submission is refused, nothing is persisted, and they are sent to the sign-in page              |
| EC-4  | An Admin demotes themselves while other active Admins exist                  | Allowed; their admin capabilities disappear on their next request                                   |
| EC-5  | Two Admins concurrently deactivate each other, leaving zero active Admins    | Serialised so that the second action is refused; the last-Admin invariant holds under concurrency   |
| EC-6  | Two Admins concurrently change the same Person's role                        | Last write wins; no error is surfaced                                                               |
| EC-7  | The correct password is supplied during an active 15-minute lockout          | Refused; the lockout window is **not** extended by further attempts                                 |
| EC-8  | A Person signs in on two devices and signs out on one                        | The other session remains valid; sessions are independent                                           |
| EC-9  | A new password below 12 characters is submitted                              | Rejected with the length rule stated explicitly; the existing password is unchanged                 |
| EC-10 | On a forced change, the new password equals the current one                  | Rejected; the person must actually change it                                                        |
| EC-11 | A full name or email exceeding its maximum length is submitted               | Rejected with a validation message naming the limit                                                 |
| EC-12 | A session expires while an entry is being edited                             | Redirected to sign-in, then returned to that page (NFR-008); unsaved edits are lost — accepted      |
| EC-13 | The seeded Admin has never signed in and its initial password is still in use | First sign-in is blocked on choosing a new password (FR-014); the seeded password stops working     |
| EC-14 | A deactivated Person is reactivated                                          | They sign in with their previous password; `deactivatedAt` is cleared; their history was never touched |

## 8. Success Criteria

<!-- Success criteria are prefixed SUC- to avoid colliding with the SC- acceptance
     scenario identifiers in Section 4. -->

| ID      | Criterion                                                                                                   |
| ------- | ------------------------------------------------------------------------------------------------------------ |
| SUC-001 | All acceptance scenarios SC-001 to SC-021 pass automatically in CI                                           |
| SUC-002 | An automated sweep confirms that no page other than sign-in is reachable without an authenticated session    |
| SUC-003 | Every capability above the User role is refused when requested directly, verified per route for all three roles |
| SUC-004 | An Admin can give a new colleague a working account in under 2 minutes with no database access               |
| SUC-005 | No password appears anywhere in the database, logs, or error output in plaintext or reversible form          |
| SUC-006 | Deactivating a Person changes no figure in any existing report or project burn-down                          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Story 001 (manual time entry)** depends on this spec for entry ownership (FR-005);
  this spec defines the owning reference that 001 consumes.
- **Stories 009, 010, 011, 015** depend on the role model in Section 2.1.
- **Story 009 (project assignment)** must not be read as an authorization mechanism —
  see the *Roles are global* invariant.
- Deployment must supply the seeded Admin's initial credentials (FR-020).
- A persistence mechanism must be chosen before implementation; this spec assumes a
  store capable of a unique constraint on email and of serialising the last-Admin
  check (EC-5), but does not require a specific technology. *Resolved at
  implementation: EF Core over SQLite, with the last-Admin check serialised in
  process — see [ADR-004](../architecture/09-architecture-decisions.md).*

### 9.2 Constraints

- Single internal organisation; no external identity provider (decision, 2026-09-15).
- No email infrastructure is available, which is what makes password recovery
  impossible in this slice rather than merely deprioritised.
- ASP.NET Core Razor Pages on .NET 10; root namespace `pmsa`.
- **Accepted tension:** a 12-hour non-persistent session means most people sign in
  daily, which taxes the product's "log hours in seconds" goal. This was chosen
  knowingly; it is recorded in arc42 Section 11 as a risk to revisit if entry friction
  becomes a complaint.

### 9.3 Architecture References

This feature was the first to require several arc42 sections; the table below states
what each must carry. Sections 03, 04, 05, 06, 08, 09, 10, 11 and 12 have since been
written — sections 01, 02 and 07 remain empty templates and are not this feature's to
fill.

| Arc42 Section                    | Relevance to This Feature                                                                                              |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| 03. Context & Scope              | Confirms no external identity system is in the context; identity is wholly internal                                    |
| 04. Solution Strategy            | Cookie-based authentication against locally stored credentials                                                          |
| 05. Building Block View          | Introduces the authentication/authorization building block that every later feature sits behind                          |
| 06. Runtime View                 | Sign-in sequence, and the per-request identity + role resolution that makes FR-017 and FR-021 immediate                 |
| 08. Cross-cutting Concepts       | **Primary section.** Authorization is crosscutting — every subsequent story is constrained by the matrix in Section 2.1 |
| 09. Architecture Decisions       | Needs ADRs for: local accounts over SSO; a flat three-role model over per-project roles; password hashing choice        |
| 10. Quality Requirements         | NFR-001 to NFR-005 are project-wide and should be promoted here rather than kept per-feature                            |
| 11. Risks & Technical Debt       | Daily re-authentication versus the speed goal; absence of password recovery                                             |
| 12. Glossary                     | Person, Role, User, Manager, Admin, active/deactivated, Session                                                         |

## 10. Open Questions

| #   | Question                                                                            | Owner | Status   | Resolution                                                                 |
| --- | ----------------------------------------------------------------------------------- | ----- | -------- | -------------------------------------------------------------------------- |
| 1   | Which story picks up password recovery, given that no one can currently be unlocked? | Product | Deferred | Out of scope here by decision; needs a place in the story map before release |
| 2   | How are the seeded Admin's initial credentials supplied at deployment time?          | Architecture | Resolved | Configuration section `SeedAdmin` (`SeedAdmin__Email` / `SeedAdmin__Password` environment variables in production). No default credential ships. See [ADR-005](../architecture/09-architecture-decisions.md) |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
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
