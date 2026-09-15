# 5. Building Block View

## Level 1: Overall System

```
┌─────────────────────────────────────────────────────────┐
│ pmsa (single ASP.NET Core process)                      │
│                                                         │
│  ┌───────────┐                                          │
│  │  Pages/   │  Razor Pages — bind, call one service,   │
│  │           │  render. No domain rules.                │
│  └─────┬─────┘                                          │
│        │                                                │
│  ┌─────▼─────┐                                          │
│  │ Security/ │  Authentication, authorization,          │
│  │           │  application services                    │
│  └─────┬─────┘                                          │
│        │                                                │
│  ┌─────▼─────┐        ┌───────────┐                     │
│  │  Domain/  │◄───────│   Data/   │  DbContext,         │
│  │           │        │           │  migrations, seeder │
│  └───────────┘        └─────┬─────┘                     │
└─────────────────────────────┼───────────────────────────┘
                              ▼
                        SQLite file
```

| Building Block | Description |
| -------------- | ----------- |
| `Domain/` | `Person`, `Role`, `EmailAddress`, `PasswordPolicy`. Holds the rules of spec 007 Section 5.4 and depends on nothing. |
| `Data/` | `PmsaDbContext` (unique index on email, `Role` persisted as text), EF migrations, `AdminSeeder`. |
| `Security/` | The authentication building block every later feature sits behind: hashing, sign-in, the per-request revalidation, the authorization policies, and the services that administer people. |
| `Pages/` | Screens. `Account/` is the signed-out and self-service area; `Admin/People/` is Admin-only by folder convention. |

## Level 2: Component Details

### Security/

| Type | Purpose |
| ---- | ------- |
| `IPasswordHasher` / `Pbkdf2PasswordHasher` | Hash, verify, and `VerifyDummy` — the last existing so an unknown email costs the same time as a known one (NFR-003). Stored form: `pbkdf2-sha256$<iterations>$<salt>$<hash>`. |
| `SignInService` | The whole of FR-006 to FR-008 in one place: normalise, look up, refuse inactive or locked-out accounts with the same message as a wrong password, count failures, lock at five for fifteen minutes, clear the counter on success. |
| `PersonClaims` | Builds the principal from a `Person` and reads it back (`GetPersonId`, `GetRole`, `IsAdmin`, `MustChangePassword`). The single place claim names are known. |
| `RevalidatePrincipalEvents` | `CookieAuthenticationEvents.ValidatePrincipal`. Reloads the `Person` each request, signs out if they are gone or deactivated, otherwise replaces the principal with current role and flags. This is what makes FR-017 and FR-021 immediate. |
| `AuthenticationDefaults` / `AuthorizationPolicies` | Scheme name, cookie name, session lifetime, the well-known paths, and the `RequireManager` / `RequireAdmin` policies. |
| `ForcePasswordChangeFilter` | A global page filter: an authenticated person carrying the must-change claim is redirected to the change-password page from anywhere except sign-out, the change page itself, and the error pages (FR-014). |
| `NoStoreFilter` | A global page filter setting `Cache-Control: no-store` so a signed-out browser's Back button cannot render a page from cache (NFR-005). |
| `PasswordChangeService` | Self-service change: current password required, policy applied, new password must differ from the current one. |
| `PeopleAdministrationService` | Create, change role, deactivate, reactivate, reset password. All four mutating operations funnel through one method that holds `AdminInvariantLock` and a transaction while it checks the last-Admin invariant. |
| `AdminInvariantLock` | A process-wide `SemaphoreSlim(1,1)`. Correct because the deployment is one instance over one SQLite file; if that ever changes it must become a database-level lock — see [ADR-004](09-architecture-decisions.md). |

### Pages/

| Page | Access | Notes |
| ---- | ------ | ----- |
| `Account/SignIn` | Anonymous | Signs out any live session on GET, so arriving at the sign-in page never leaves a half-state. |
| `Account/SignOut` | Authenticated | POST only; a GET redirects. Sign-out is a state change and must not be triggerable by a link or a prefetch. |
| `Account/ChangePassword` | Authenticated | Serves both the forced and the voluntary change. Re-issues the cookie on success (NFR-004). |
| `Account/Forbidden` | Authenticated | Responds 403 and says nothing about what was behind the refusal (FR-010). |
| `Admin/People/*` | `RequireAdmin`, by `AuthorizeFolder` | Folder-level, so a new page added here is protected without anyone remembering to attribute it. |

## Level 3: Internal Structure (if needed)

Nothing below level 2 is architecturally significant yet.
