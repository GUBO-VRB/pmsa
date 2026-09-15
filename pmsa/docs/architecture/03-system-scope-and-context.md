# 3. System Scope and Context

## Business Context

pmsa is a self-contained internal application. It has no external communication
partners: there is no identity provider, no email gateway, no payroll or invoicing
system it feeds. Everything it knows about a person is stored in its own database and
administered from inside its own screens.

That is a deliberate scope decision rather than an omission — see
[ADR-001](09-architecture-decisions.md) — and it is what makes several rules in
[spec 007](../specs/007-authentication-and-roles.md) possible in the form they take
(e.g. an Admin resets a password face to face, because there is no mailbox to send a
reset link to).

```
          ┌──────────────┐
          │  Person      │  signs in, logs hours, reads reports
          │ (User /      │
          │  Manager /   │
          │  Admin)      │
          └──────┬───────┘
                 │ HTTPS (browser)
                 ▼
          ┌──────────────┐
          │    pmsa      │
          └──────┬───────┘
                 │ local file I/O
                 ▼
          ┌──────────────┐
          │  SQLite file │
          └──────────────┘
```

| Communication Partner | Inputs | Outputs |
| --------------------- | ------ | ------- |
| Person (any role) | Credentials, time entries, project and report criteria | Their own day and week, reports scoped to their role, error and refusal messages |
| Admin | Account details, role assignments, activation state, assigned passwords | The list of people and the outcome of each administrative action |
| Deployment / operator | `SeedAdmin:Email` and `SeedAdmin:Password` configuration, connection string | Startup log stating whether the initial Admin was seeded or skipped |

**Explicitly outside the context:** corporate SSO or any external IdP, an SMTP server,
an HR system as a source of people. Their absence is what forces the seeded-Admin
bootstrap (FR-020) and rules out self-service password recovery.

## Technical Context

| Technical Interface | Description |
| ------------------- | ----------- |
| HTTPS + HTML forms | The only inbound channel. Razor Pages server-rendered; no public API, no SPA, no mobile client. |
| Session cookie `pmsa.session` | Encrypted ASP.NET Core authentication cookie: `HttpOnly`, `Secure`, `SameSite=Lax`, non-persistent, 12-hour absolute expiry. Carries an identity reference only — never a password or a role decision that is trusted without revalidation. |
| SQLite database file | Single-file store reached through EF Core. Implies a single application instance; see [ADR-004](09-architecture-decisions.md). |
| Configuration / environment | Connection string, `PasswordHashing:Iterations`, and the seed credentials, supplied per environment (`SeedAdmin__Email` / `SeedAdmin__Password` as environment variables in production). |
