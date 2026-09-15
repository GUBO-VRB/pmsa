using Microsoft.EntityFrameworkCore;
using pmsa.Data;
using pmsa.Domain;

namespace pmsa.Security;

public enum PasswordChangeOutcome
{
    Succeeded,
    CurrentPasswordIncorrect,
    NewPasswordRejected,
    SameAsCurrent,
    NotFound,
}

public record PasswordChangeResult(PasswordChangeOutcome Outcome, string? Message = null, Person? Person = null)
{
    public const string CurrentPasswordIncorrectMessage = "The current password is incorrect.";
    public const string SameAsCurrentMessage = "The new password must be different from the current one.";

    public bool Succeeded => Outcome is PasswordChangeOutcome.Succeeded;
}

/// <summary>
/// A person changing their own password (FR-019), which is also how a forced change is resolved
/// (FR-014).
/// </summary>
public class PasswordChangeService(PmsaDbContext db, IPasswordHasher hasher)
{
    public async Task<PasswordChangeResult> ChangeAsync(
        Guid personId, string? currentPassword, string? newPassword, CancellationToken cancellationToken = default)
    {
        var person = await db.People.SingleOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return new PasswordChangeResult(PasswordChangeOutcome.NotFound);
        }

        // FR-019: the current password is required even during a forced change, so that an
        // unattended signed-in browser cannot be used to take the account over.
        if (!hasher.Verify(currentPassword ?? string.Empty, person.PasswordHash))
        {
            return new PasswordChangeResult(
                PasswordChangeOutcome.CurrentPasswordIncorrect,
                PasswordChangeResult.CurrentPasswordIncorrectMessage);
        }

        if (PasswordPolicy.Validate(newPassword) is { } problem)
        {
            return new PasswordChangeResult(PasswordChangeOutcome.NewPasswordRejected, problem);
        }

        // EC-10: on a forced change the person must actually change it. Applied to voluntary
        // changes too — re-submitting the same password is never what was meant.
        if (currentPassword == newPassword)
        {
            return new PasswordChangeResult(PasswordChangeOutcome.SameAsCurrent, PasswordChangeResult.SameAsCurrentMessage);
        }

        person.SetPassword(hasher.Hash(newPassword!));
        await db.SaveChangesAsync(cancellationToken);

        return new PasswordChangeResult(PasswordChangeOutcome.Succeeded, Person: person);
    }
}
