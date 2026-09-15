namespace pmsa.Security;

/// <summary>
/// Spec 007, NFR-001 and the <em>Passwords are irreversible</em> invariant: nothing in the
/// system can turn a stored value back into a password.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a password with a freshly generated per-person salt.</summary>
    string Hash(string password);

    /// <summary>Constant-time comparison of a candidate password against a stored hash.</summary>
    bool Verify(string password, string storedHash);

    /// <summary>
    /// Performs the same work as <see cref="Verify"/> without a real hash to compare against, so
    /// that an unknown email address costs the same as a wrong password (NFR-003).
    /// </summary>
    void VerifyDummy(string password);
}
