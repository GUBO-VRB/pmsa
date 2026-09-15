# 9. Architecture Decisions

<!-- Important, expensive, large-scale, or risky architecture decisions. -->

## Decision Log

| ID  | Decision | Status | Date |
| --- | -------- | ------ | ---- |
| ADR-001 | Local accounts, not an external identity provider | Accepted | 2026-09-15 |
| ADR-002 | Cookie authentication over a domain-owned `Person`, not ASP.NET Core Identity | Accepted | 2026-09-15 |
| ADR-003 | PBKDF2-HMAC-SHA256 with a self-describing stored format | Accepted | 2026-09-15 |
| ADR-004 | A flat three-role model, and an in-process lock for the last-Admin invariant | Accepted | 2026-09-15 |
| ADR-005 | Seed credentials come from configuration; no default account exists | Accepted | 2026-09-15 |

---

### ADR-001: Local accounts, not an external identity provider

**Status:** Accepted, 2026-09-15 · **Context:** [spec 007](../specs/007-authentication-and-roles.md) §9.2

pmsa serves a single internal organisation. No SSO tenant was offered to integrate
with, and no email infrastructure exists.

**Decision.** Store credentials locally and authenticate against them.

**Consequences.** The system owns password strength, lockout and reset entirely — and
also owns the risk of holding password hashes. Without a mailbox to send to, there is
no self-service recovery: a locked-out or forgotten-password person is unblocked by an
Admin in person. That is an accepted product consequence, recorded as a risk in
Section 11, not a gap to be filled quietly later. Should an IdP appear, `Person`
survives as the profile and role record; only the credential columns and
`SignInService` are displaced.

---

### ADR-002: Cookie authentication over a domain-owned `Person`, not ASP.NET Core Identity

**Status:** Accepted, 2026-09-15

Identity would supply user storage, hashing, lockout and sign-in management out of the
box. But spec 007 already specifies all four with its own semantics — a 200-character
display name, a forced-change flag, a deactivation that preserves history, a lockout
that is explicitly *not* extended by attempts made during it — and `Person` is the
entity every later story references.

**Decision.** Use `AddAuthentication().AddCookie()` directly and keep `Person`,
`SignInService` and `Pbkdf2PasswordHasher` in the domain and security layers.

**Consequences.** More code written here, but one model of a person rather than two,
and every rule in spec 007 §5.4 lives where the tests can reach it without a host. We
forgo Identity's ready-made 2FA, external logins and token providers; none are in
scope, and adopting them later means adopting Identity's schema then, not now.

---

### ADR-003: PBKDF2-HMAC-SHA256 with a self-describing stored format

**Status:** Accepted, 2026-09-15 · **Context:** NFR-001

Argon2id is the stronger algorithm, but it needs a third-party package
(e.g. Konscious.Security.Cryptography). PBKDF2 is in the BCL.

**Decision.** PBKDF2-HMAC-SHA256, 600 000 iterations (OWASP guidance for SHA-256), a
128-bit per-person salt, constant-time comparison. Store as
`pbkdf2-sha256$<iterations>$<salt>$<hash>`.

**Consequences.** Weaker against GPU attack than a memory-hard function, accepted for
an internal tool with no external exposure. Because the stored value names its own
algorithm and cost, the iteration count can be raised — or a second algorithm added —
and existing hashes continue to verify, upgrading on next sign-in. The iteration count
is configurable (`PasswordHashing:Iterations`), which also lets the test suite run at a
cost that does not dominate its runtime.

---

### ADR-004: A flat three-role model, and an in-process lock for the last-Admin invariant

**Status:** Accepted, 2026-09-15 · **Context:** spec 007 §2.1, EC-5, EC-6

Per-project roles ("Manager *of project X*") were considered, since project assignment
(story 009) already names people against projects.

**Decision.** Roles are global: User, Manager, Admin. Project assignment decides what
a person may book hours to and is explicitly *not* an authorization mechanism.

**Decision (invariant).** The "at least one active Admin" rule is enforced by holding
a process-wide `SemaphoreSlim` across a database transaction that mutates the person
and re-checks the rule before committing.

**Consequences.** The role model is trivially explainable and a permission question
has one answer per person rather than one per person-project pair; the cost is that a
Manager manages everything or nothing, which is right for one internal team and would
not survive multiple departments.

The semaphore is correct **only while the deployment is a single process**, which
SQLite already implies. If pmsa is ever scaled out or moved to a server database, this
must become a database-level guard (a serialisable transaction or a dedicated lock
row) — this is the single assumption most likely to be invalidated by a deployment
change, and it is listed in Section 11.

Concurrent *edits* to the same person (EC-6) are deliberately unguarded: no row
version, last write wins. A lost role edit between two Admins is cheaper to re-apply
than a conflict screen is to explain.

---

### ADR-005: Seed credentials come from configuration; no default account exists

**Status:** Accepted, 2026-09-15 · **Resolves:** spec 007 open question 2

A fresh deployment has no accounts, and nothing in the system can create the first one
from inside it (FR-020).

**Decision.** At startup, after migrations, seed one Admin from the `SeedAdmin`
configuration section — `SeedAdmin__Email` / `SeedAdmin__Password` as environment
variables in production. The account is created with the forced-change flag armed, so
the supplied password is spent on first use. Seeding is skipped entirely if a person
with that address already exists, which makes redeployment safe (NFR-007). If the
section is missing or invalid, the application starts with no accounts and logs it.

**Consequences.** No shipped default credential can ever be left in place — the
common failure mode of seeded-admin designs. The cost is that a deployment with
forgotten seed configuration starts with no way in; the startup log says so
explicitly, and the fix is to set the variables and restart. Development uses
`appsettings.Development.json` with an obviously local address and password.
