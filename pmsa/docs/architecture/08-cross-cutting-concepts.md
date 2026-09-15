# 8. Cross-cutting Concepts

## Domain Model

`Person` is the identity every other entity in the system will hang from: a time
entry is *someone's*, a report is scoped to *someone*, a project assignment names
*someone*. It is introduced by [spec 007](../specs/007-authentication-and-roles.md)
Section 5 and is the reason story 007 was specified ahead of story 001.

```
Person
  Id            Guid (v7)         identity referenced by every later entity
  FullName      string(200)       shown wherever an action is attributed
  Email         string(254)       unique, normalised lowercase — the sign-in name
  PasswordHash  string            never the password; see Security below
  Role          User | Manager | Admin
  IsActive      bool              deactivated people keep their history
  MustChangePassword bool         armed on creation and on an Admin reset
  FailedAttemptCount / LockedUntil
  CreatedAt / DeactivatedAt
```

The entity owns its rules rather than exposing setters: `RecordFailedSignIn` decides
when a lockout starts, `SetPassword` versus `SetAdminAssignedPassword` decides whether
the forced-change flag is cleared or armed, `Deactivate` stamps the time. A caller
cannot perform half of one of these operations.

**Invariants** (spec 007, Section 5.4):

- *Unique identity* — one active-or-not `Person` per normalised email address. A
  leaver's address is never re-bound, because rebinding it would silently reattribute
  their history to someone else.
- *Roles are global* — a role is held over the whole system, not per project. Project
  assignment (story 009) decides what you may book hours *to*; it is not an
  authorization mechanism.
- *At least one active Admin* — the system cannot be administered into a state where
  no one can administer it.
- *Passwords are irreversible* — nothing in the system can recover a password, only
  replace it.

## Security

**Authentication.** A cookie, `pmsa.session`, issued by the ASP.NET Core cookie
handler under the scheme `pmsa`: `HttpOnly`, `Secure`, `SameSite=Lax`, non-persistent,
12-hour *absolute* expiry with sliding expiration off. It carries an identity
reference; it is not a bearer of authority by itself.

**Authorization** is the crosscutting concern of this system — every story in the map
is constrained by it, so the mechanism matters more than any single rule it enforces.
Three properties:

1. **Deny by default.** A fallback policy requires an authenticated user, so a page
   added tomorrow with no attribute is protected. Anonymity is written out explicitly
   and the list is short: sign-in and the error page.
2. **Folder-level for capabilities.** `/Admin` is guarded by `AuthorizeFolder`, not by
   an attribute per page model, so protection is not something a new page can forget.
3. **Never trusted from the cookie.** The role acted on is the role read from the
   database on the current request (Section 6, Scenario 2). A stale cookie cannot
   carry a privilege that has since been withdrawn.

| | User | Manager | Admin |
| --- | --- | --- | --- |
| Own entries and own week | ✔ | ✔ | ✔ |
| Others' entries, cross-person reports | ✗ | ✔ | ✔ |
| Projects, budgets, assignments | ✗ | ✔ | ✔ |
| People, roles, activation | ✗ | ✗ | ✔ |

The Manager column is enforced today by the `RequireManager` policy; the capabilities
it guards arrive with stories 009–011 and 012–016.

**Credentials.** PBKDF2-HMAC-SHA256, 600 000 iterations, 128-bit per-person salt,
compared with `CryptographicOperations.FixedTimeEquals`. Stored as
`pbkdf2-sha256$<iterations>$<base64 salt>$<base64 hash>` — self-describing, so the
iteration count can be raised later and old hashes still verify. An unreadable stored
value verifies as `false` rather than throwing.

**Refusal discloses nothing.** Every failed sign-in returns one message regardless of
cause, and the "no such person" path still performs a dummy verification so it costs
the same as a real one. A refused authorization returns 403 with no hint of what lay
behind it.

**Account protection.** Five consecutive failures lock an account for fifteen
minutes. Attempts made *during* a lockout do not extend it — otherwise an attacker
could keep an account locked indefinitely and the lockout becomes the attack.

## Persistence

EF Core over a single SQLite file. Migrations live in `Data/Migrations/` and are
applied at startup (`MigrateAsync`) before the seeder runs. `Role` is persisted as
text rather than an integer so the database stays readable and a reordering of the
enum cannot silently re-grade people.

The unique index on `Email` is the enforcement point for the *unique identity*
invariant — the application check is a courtesy that produces a good message, and the
database is what makes the rule true under concurrency.

## Logging and Monitoring

Sign-in outcomes are logged with the cause the screen deliberately withholds
(unknown address, wrong password, deactivated, locked out), which is the only place
that distinction exists. Administrative actions and seeding decisions are logged.
Passwords, hashes and cookie values are never logged.

## Error Handling

Expected outcomes are values, not exceptions: `SignInAttemptResult`,
`AdministrationResult` and `PasswordChangeResult` each carry an outcome enum and a
message the page can render. Exceptions are reserved for genuine faults, and reach
the framework's error page. A refused operation always leaves the store unchanged.

## Configuration

Per-environment settings are `ConnectionStrings:Pmsa`, `PasswordHashing:Iterations`
and the `SeedAdmin` section. The seeded Admin's credentials are supplied as
configuration — `SeedAdmin__Email` / `SeedAdmin__Password` environment variables in
production — and are never committed. This resolves open question 2 of spec 007.
If the section is absent or invalid the application starts with no accounts and logs
that it did; it does not invent a default credential.

## Testing

Acceptance scenarios SC-001..SC-021 are xUnit tests in the sibling `pmsa.Tests`
project, driving the real HTTP pipeline through `WebApplicationFactory<Program>` —
form posts with anti-forgery tokens, real cookies, real redirects — because a test
that bypassed the pipeline would not exercise the thing being specified. Each test
class gets its own in-memory SQLite database, and a `FakeTimeProvider` is injected as
the application's `TimeProvider` *and* the cookie handler's, so a 12-hour session
expiry or a 15-minute lockout is tested by advancing a clock rather than waiting.
Domain rules that need no host (email normalisation, password policy, lockout
arithmetic, hashing) are plain unit tests.

## Build and Deployment

`dotnet build` / `dotnet run` from the project directory; there is no solution file
and no CI pipeline yet. Migrations are applied by the application at startup, so a
deployment is: publish, set the environment variables, start.
