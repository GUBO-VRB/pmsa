# 6. Runtime View

## Scenario 1: Sign-in

Covers spec 007 FR-006 to FR-008 and FR-014.

```
Browser            SignIn page        SignInService      PasswordHasher      Database
   │                    │                   │                  │                │
   │─ POST credentials ─►                   │                  │                │
   │                    │─ Authenticate ────►                  │                │
   │                    │                   │─ find by normalised email ────────►
   │                    │                   │◄──────────────────────── Person? ──│
   │                    │                   │                  │                │
   │                    │      none / inactive / locked out    │                │
   │                    │                   │─ VerifyDummy ────►   (equal cost)  │
   │                    │◄── Refused ───────│                  │                │
   │                    │                   │                  │                │
   │                    │      otherwise    │─ Verify ─────────►                │
   │                    │                   │◄── false ────────│                │
   │                    │                   │─ RecordFailedSignIn, save ────────►
   │                    │◄── Refused ───────│    (5th failure ⇒ locked 15 min)   │
   │                    │                   │                  │                │
   │                    │                   │◄── true ─────────│                │
   │                    │                   │─ RecordSuccessfulSignIn, save ────►
   │                    │◄── Succeeded ─────│                  │                │
   │                    │                                                       │
   │                    │─ SignInAsync (non-persistent cookie, 12 h absolute)    │
   │◄─ 302 ─────────────│   to /Account/ChangePassword if MustChangePassword,    │
   │                    │   else the local returnUrl, else the day screen        │
```

Every refusal — unknown address, wrong password, deactivated account, locked-out
account — returns the *same* message and the same status. The distinction exists in
the log, not on the screen (FR-007, NFR-002).

## Scenario 2: Any authenticated request

This is the scenario that makes FR-017 (role change) and FR-021 (deactivation) take
effect immediately, and it runs on *every* request, not just protected ones.

```
Browser ── cookie ──► Cookie handler
                          │
                          │ ValidatePrincipal
                          ▼
                   RevalidatePrincipalEvents
                          │
                          ├─ decrypt ticket → person id
                          ├─ load Person (no tracking)
                          │
                          ├─ missing or deactivated ──► RejectPrincipal + SignOutAsync
                          │                              ⇒ next authorization refuses;
                          │                                the browser is signed out
                          │
                          └─ otherwise ──► ReplacePrincipal(fresh role + flags)
                                           ShouldRenew = false
                                                 │
                                                 ▼
                                   Authorization (fallback policy,
                                   RequireManager / RequireAdmin)
                                                 │
                                                 ▼
                                   ForcePasswordChangeFilter → NoStoreFilter → page
```

`ShouldRenew = false` is load-bearing: replacing the principal must not re-issue the
cookie, or the 12-hour expiry would slide and FR-007's fixed session would become a
rolling one.

## Error/Edge Case Scenarios

### Two Admins demoted at the same moment (EC-5)

The invariant "at least one active Admin" cannot be checked and then acted on
separately, or both requests read "another Admin exists" and both proceed.

```
Request A ──► AdminInvariantLock.AcquireAsync ──► BEGIN TRANSACTION
                 mutate Person in memory
                 does another active Admin still exist?  ── yes ─► SAVE, COMMIT, release
Request B ──► (blocked on the semaphore) ...
              ──► BEGIN TRANSACTION
                 mutate Person in memory
                 does another active Admin still exist?  ── no ──► reload entity,
                                                                   ROLLBACK, release,
                                                                   return WouldRemoveLastAdmin
```

One succeeds, one is refused with an explanation. The losing request reloads its
entity before rolling back so the scoped `DbContext` is not left holding a mutation
that was never committed.

### Two Admins editing the same person (EC-6)

Deliberately *not* guarded. There is no row version and no concurrency exception:
last write wins. The spec treats a lost role edit between two Admins as cheaper to
re-apply than a conflict screen is to explain.

### A session that expires mid-work (SC-005, SC-007)

The cookie's absolute expiry passes, the handler stops producing an identity, the
fallback policy refuses, and the person is redirected to sign-in with `returnUrl`
pointing back at what they were doing. After signing in they land where they were,
not on the home page.
