using Microsoft.EntityFrameworkCore;
using pmsa.Data;
using pmsa.Domain;

namespace pmsa.Security;

public enum SignInOutcome
{
    Succeeded,

    /// <summary>Unknown email, wrong password, deactivated account, or an active lockout.</summary>
    Refused,
}

public record SignInAttemptResult(SignInOutcome Outcome, Person? Person)
{
    /// <summary>
    /// FR-002 and FR-003: one message for every refusal, so it never discloses whether the email
    /// address exists, whether the account is active, or whether it is locked out.
    /// </summary>
    public const string RefusalMessage = "Email address or password is incorrect.";

    public bool Succeeded => Outcome is SignInOutcome.Succeeded;
}

/// <summary>
/// Authenticates a person by email address and password (FR-001 to FR-003, FR-008).
/// </summary>
public class SignInService(PmsaDbContext db, IPasswordHasher hasher, TimeProvider clock)
{
    public async Task<SignInAttemptResult> AuthenticateAsync(
        string? email, string? password, CancellationToken cancellationToken = default)
    {
        var normalisedEmail = EmailAddress.Normalise(email);
        var now = clock.GetUtcNow();

        var person = await db.People.SingleOrDefaultAsync(p => p.Email == normalisedEmail, cancellationToken);

        // NFR-003: an unknown email must cost the same as a wrong password, so the dummy
        // derivation runs before every early return below.
        if (person is null)
        {
            hasher.VerifyDummy(password ?? string.Empty);
            return new SignInAttemptResult(SignInOutcome.Refused, null);
        }

        // FR-003: a deactivated account is refused without even considering the password, and
        // without a message that distinguishes it (SC-004).
        if (!person.IsActive)
        {
            hasher.VerifyDummy(password ?? string.Empty);
            return new SignInAttemptResult(SignInOutcome.Refused, null);
        }

        // EC-7: during a lockout the correct password is refused and the window is not extended,
        // which is why nothing is recorded on this path.
        if (person.IsLockedOut(now))
        {
            hasher.VerifyDummy(password ?? string.Empty);
            return new SignInAttemptResult(SignInOutcome.Refused, null);
        }

        if (!hasher.Verify(password ?? string.Empty, person.PasswordHash))
        {
            person.RecordFailedSignIn(now);
            await db.SaveChangesAsync(cancellationToken);
            return new SignInAttemptResult(SignInOutcome.Refused, null);
        }

        person.RecordSuccessfulSignIn();
        await db.SaveChangesAsync(cancellationToken);
        return new SignInAttemptResult(SignInOutcome.Succeeded, person);
    }
}
