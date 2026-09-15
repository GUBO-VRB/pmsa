# 4. Solution Strategy

## Technology Decisions

| Decision | Rationale |
| -------- | --------- |
| ASP.NET Core Razor Pages, .NET 10 | Server-rendered pages keep every authorization decision on the server. A page-per-screen structure matches a story map whose stories *are* screens. |
| EF Core + SQLite | One file to back up, no server to operate, adequate for an internal team-sized tool. Supplies the unique constraint on email and the transaction the last-Admin check needs. Its single-writer nature is accepted, not worked around — [ADR-004](09-architecture-decisions.md). |
| Cookie authentication against locally stored credentials | No external identity provider is in the context (Section 3). The framework's cookie handler is used directly rather than ASP.NET Core Identity, because the domain owns its own `Person` entity, lockout policy and password rules, and Identity's schema would duplicate all of them — [ADR-002](09-architecture-decisions.md). |
| PBKDF2-HMAC-SHA256, 600 000 iterations | Built into the platform, no third-party dependency, OWASP-current parameters. The stored format carries its own iteration count so the cost can be raised later without invalidating existing hashes — [ADR-003](09-architecture-decisions.md). |
| Rich domain entities over anemic records | `Person` enforces its own lockout, forced-change and activation rules in one place, so no call site can forget them. |

## Top-level Decomposition

Four layers, each a folder in the single project (see Section 5):

- `Domain/` — entities and value rules, no framework dependencies.
- `Data/` — `PmsaDbContext`, migrations, and the startup seeder.
- `Security/` — authentication, authorization, and the application services that
  mutate `Person` under the domain's invariants.
- `Pages/` — Razor Pages. Thin: they bind input, call one service, and render.

## Approaches to Achieve Quality Goals

| Quality Goal | Approach |
| ------------ | -------- |
| Nothing is reachable unauthenticated | A *fallback* authorization policy requiring an authenticated user, so a new page is protected by default. Anonymous access is the exception and must be written out explicitly (`AllowAnonymousToPage`). |
| A role change or a deactivation takes effect now | The cookie carries an identity reference, not a trusted role. `ValidatePrincipal` rebuilds the principal from the database on every request, with `ShouldRenew = false` so the refresh does not slide the absolute expiry. |
| Credentials are irreversible and slow to guess | Per-person salt, 600 000 PBKDF2 iterations, constant-time comparison, and a dummy verification on the "no such person" path so an unknown address costs the same as a known one. |
| The system can never lock itself out | The last active Admin cannot be removed. The check and the write are serialised together (semaphore + transaction), so two concurrent demotions cannot both pass it. |
| Acceptance scenarios stay true | Each of SC-001..SC-021 is an xUnit test driving the real HTTP pipeline through `WebApplicationFactory`, with a `FakeTimeProvider` so session expiry and lockout windows are tested by advancing a clock rather than waiting. |

## Organizational Decisions

Work flows story map → spec → implementation, as described in `CLAUDE.md`. A spec's
`NNN` prefix is the story number, and that number is the thread from map to spec to
the tests that prove it. Architectural decisions found during implementation are
logged in Section 9 rather than left in the spec.
