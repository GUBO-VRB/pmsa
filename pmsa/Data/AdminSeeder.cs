using Microsoft.EntityFrameworkCore;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Data;

/// <summary>Deployment-supplied credentials for the first Admin account (FR-020).</summary>
public class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string FullName { get; set; } = "Administrator";
}

/// <summary>
/// Creates the seeded Admin on first deployment (FR-020).
/// </summary>
/// <remarks>
/// NFR-007 makes idempotence the hard requirement: redeploying must never reset an existing
/// Admin's password or reactivate one that was deactivated. That is why the only question asked
/// is "does a Person with this email exist?" — if one does, nothing is touched, whatever state it
/// is in. The seeded password is subject to FR-014, so it stops working as soon as the Admin
/// signs in and chooses their own (EC-13).
/// </remarks>
public class AdminSeeder(
    PmsaDbContext db,
    IPasswordHasher hasher,
    TimeProvider clock,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(SeedAdminOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning(
                "No seed Admin configured ({Section}:Email / {Section}:Password). " +
                "If no Admin account exists yet, nobody will be able to administer the system.",
                SeedAdminOptions.SectionName, SeedAdminOptions.SectionName);
            return;
        }

        if (!EmailAddress.IsValid(options.Email))
        {
            logger.LogError("The configured seed Admin email address is not valid; no Admin was seeded.");
            return;
        }

        if (PasswordPolicy.Validate(options.Password) is { } problem)
        {
            logger.LogError("The configured seed Admin password was rejected: {Problem}", problem);
            return;
        }

        var email = EmailAddress.Normalise(options.Email);
        if (await db.People.AnyAsync(p => p.Email == email, cancellationToken))
        {
            logger.LogDebug("Seed Admin {Email} already exists; leaving it untouched.", email);
            return;
        }

        db.People.Add(Person.Create(
            options.FullName, email, hasher.Hash(options.Password), Role.Admin, clock.GetUtcNow()));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded the initial Admin account {Email}.", email);
        }
        catch (DbUpdateException)
        {
            // Two instances starting at once: the unique index settled it, and the winner's
            // account is the one we wanted anyway.
            logger.LogDebug("Seed Admin {Email} was created concurrently; leaving it untouched.", email);
        }
    }
}
