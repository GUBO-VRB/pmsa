# 10. Quality Requirements

> The scenarios below were promoted out of
> [spec 007](../specs/007-authentication-and-roles.md) (NFR-001 to NFR-005) because
> they are project-wide, not properties of the authentication feature: every screen
> added from story 001 onward is bound by them. Later specs should reference this
> section rather than restate them.
>
> Section 1's quality-goal table is still empty; when it is filled, goals Q1–Q4 below
> are the candidates this feature established.

## Quality Tree

```
Quality
├── Q1 Confidentiality — no one acts as, or sees the work of, someone else
│   ├── QS-01 Unauthenticated request to any page
│   ├── QS-02 Authenticated request above the caller's role
│   └── QS-03 Signed-out browser pressing Back
├── Q2 Credential safety — a copy of the database is not a copy of the passwords
│   ├── QS-04 Stored credential form
│   └── QS-05 Refusal discloses nothing
├── Q3 Timeliness of authority — a withdrawn privilege is withdrawn now
│   ├── QS-06 Role change mid-session
│   └── QS-07 Deactivation mid-session
└── Q4 Operability — the system cannot be administered into an unusable state
    ├── QS-08 Last active Admin
    └── QS-09 Redeployment
```

## Quality Scenarios

| ID | Quality Goal | Scenario | Expected Response | Priority |
| --- | --- | --- | --- | --- |
| QS-01 | Q1 Confidentiality | An unauthenticated browser requests any page in the application, including one added after this section was written | Redirected to sign-in with `returnUrl`. Protection is the default: a page must opt *out* of it explicitly (fallback authorization policy). No page content is rendered. | High |
| QS-02 | Q1 Confidentiality | A signed-in person addresses a capability above their role directly by URL, by GET or POST | 403, no data from behind the refusal, no indication of what exists there. The action does not occur. | High |
| QS-03 | Q1 Confidentiality | A person signs out on a shared machine; the next user presses Back | No authenticated page is re-rendered from cache. Every page is served `Cache-Control: no-store` (was NFR-005). | Medium |
| QS-04 | Q2 Credential safety | An attacker obtains the database file | No password is recoverable. Credentials are stored only as a salted PBKDF2-HMAC-SHA256 hash at ≥600 000 iterations, per-person salt (was NFR-001). | High |
| QS-05 | Q2 Credential safety | An attacker probes sign-in to learn which email addresses exist | Identical message, status and timing whether the address is unknown, the password wrong, the account deactivated or locked out. The unknown-address path performs an equal-cost dummy verification (was NFR-002, NFR-003). | High |
| QS-06 | Q3 Timeliness | An Admin changes someone's role while that person is working | The new role applies to their next request, with no re-authentication and no sign-out. The cookie is an identity reference; the role is read from the store each request. | High |
| QS-07 | Q3 Timeliness | An Admin deactivates someone while that person is working | Their next request is refused and their session ends. A deactivated person cannot continue on a valid cookie. | High |
| QS-08 | Q4 Operability | Two Admins simultaneously remove the last remaining Admin privilege — by demotion, deactivation, or one of each | Exactly one action succeeds; the other is refused with an explanation and changes nothing. At least one active Admin always remains. | High |
| QS-09 | Q4 Operability | The application is redeployed over an existing database | No password is reset, no deactivated account is reactivated, no duplicate account is created. Seeding is a no-op when the seeded address already exists (was NFR-007). | Medium |
| QS-10 | Q2 Credential safety | A person changes their password | The session is re-issued rather than reused, so a cookie captured before the change carries no authority afterwards (was NFR-004). | Medium |

**Verification.** Each scenario above has at least one corresponding test in
`pmsa.Tests`; the full acceptance set SC-001..SC-021 from spec 007 runs as 75 tests.
Time-dependent scenarios are driven by a `FakeTimeProvider` rather than by waiting.
