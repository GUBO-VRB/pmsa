namespace pmsa.Domain;

/// <summary>
/// A member of the organisation who can sign in and to whom time entries belong (spec 007,
/// Section 5.1). Named <em>Person</em> rather than <em>User</em> because <see cref="Role.User"/>
/// is one of the three roles.
/// </summary>
/// <remarks>
/// A Person is never deleted — the <em>Deactivate, never delete</em> invariant. Deletion would
/// orphan time entries and retroactively change project burn-down.
/// </remarks>
public class Person
{
    /// <summary>Maximum stored length of <see cref="FullName"/>.</summary>
    public const int FullNameMaxLength = 200;

    /// <summary>Consecutive failures that trigger a lockout (FR-008).</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>How long a lockout lasts (FR-008).</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Always stored normalised — see <see cref="EmailAddress.Normalise"/>.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>One-way hash with a per-person salt; never exposed by any interface.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public Role Role { get; private set; } = Role.User;

    public bool IsActive { get; private set; } = true;

    /// <summary>While true, the Person can reach nothing but the password-change screen and sign-out.</summary>
    public bool MustChangePassword { get; private set; } = true;

    public int FailedAttemptCount { get; private set; }

    /// <summary>While in the future, sign-in is refused regardless of password.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Set when <see cref="IsActive"/> becomes false, cleared on reactivation.</summary>
    public DateTimeOffset? DeactivatedAt { get; private set; }

    private Person() { }

    /// <summary>
    /// Creates an account with an admin-set password, which the person must replace at first
    /// sign-in (FR-013, FR-014).
    /// </summary>
    public static Person Create(string fullName, string email, string passwordHash, Role role, DateTimeOffset now) =>
        new()
        {
            FullName = fullName.Trim(),
            Email = EmailAddress.Normalise(email),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = now,
        };

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && until > now;

    /// <summary>
    /// Records a failed sign-in. The lockout window is deliberately <em>not</em> extended by
    /// attempts made while already locked out (EC-7).
    /// </summary>
    public void RecordFailedSignIn(DateTimeOffset now)
    {
        if (IsLockedOut(now))
        {
            return;
        }

        FailedAttemptCount++;
        if (FailedAttemptCount >= MaxFailedAttempts)
        {
            LockedUntil = now + LockoutDuration;
            FailedAttemptCount = 0;
        }
    }

    /// <summary>Clears the failure counter and any expired lockout (FR-008).</summary>
    public void RecordSuccessfulSignIn()
    {
        FailedAttemptCount = 0;
        LockedUntil = null;
    }

    /// <summary>A person changing their own password (FR-019) or resolving a forced change (FR-014).</summary>
    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
    }

    /// <summary>An Admin setting someone's password, which the person must then replace (FR-014).</summary>
    public void SetAdminAssignedPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = true;
    }

    public void ChangeRole(Role role) => Role = role;

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAt = now;
    }

    /// <summary>
    /// Reactivation restores the previous password and clears <see cref="DeactivatedAt"/>; the
    /// person's history was never touched (EC-14).
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        DeactivatedAt = null;
        FailedAttemptCount = 0;
        LockedUntil = null;
    }

    public void Rename(string fullName) => FullName = fullName.Trim();
}
