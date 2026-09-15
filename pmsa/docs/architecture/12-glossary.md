# 12. Glossary

<!-- Domain and technical terms used throughout the documentation -->

| Term | Definition |
| ---- | ---------- |
| **Person** | The account entity: one human who can sign in. Named *Person* rather than *User* because "User" is taken by the role of the same name. Every ownership reference in the system — whose entry, whose week, whose report — points at a Person. |
| **Role** | A capability level held over the whole system, not per project. Exactly one of User, Manager or Admin. See [ADR-004](09-architecture-decisions.md). |
| **User** | The base role. Sees and edits their own time entries and their own week, and nothing of anyone else's. |
| **Manager** | User, plus other people's entries, cross-person reporting, and projects, budgets and assignments. The story map's "elevated user" is a Manager. |
| **Admin** | Manager, plus administration of people: creating accounts, assigning roles, deactivating and reactivating, resetting passwords. The only role that can change who can do what. |
| **Active / deactivated** | A Person is *active* until an Admin deactivates them. Deactivation ends their access at their next request and is not deletion: their history stays attributed to them, and their email address is never re-bound to anyone else. Reversible by an Admin. |
| **Session** | The period between a successful sign-in and its end. Carried by the `pmsa.session` cookie: non-persistent (it dies with the browser) and 12 hours absolute (it does not extend with activity). The cookie carries an identity reference, never a role that is trusted without revalidation. |
| **Lockout** | The 15-minute period after five consecutive failed sign-ins during which an account is refused even with the correct password. Attempts made during a lockout do not extend it. |
| **Forced password change** | The state of a Person whose password was set by someone else — at creation or by an Admin reset. Until they set their own, every page redirects them to the change-password screen. |
| **Seeded Admin** | The single Admin account created at startup from configuration on a deployment that has none, so a fresh system can be entered at all. Its password is spent on first use. See [ADR-005](09-architecture-decisions.md). |
| **Elevated user** | Story-map vocabulary, superseded. Reads as *Manager or Admin*; the story map now names the role directly. |
| **Story number** | The `NNN` shared by a story in `docs/product/story-map.md`, its spec file `docs/specs/NNN-*.md`, and the tests that prove it. The thread from idea to code. |
