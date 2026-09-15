using Microsoft.EntityFrameworkCore;
using pmsa.Data;
using pmsa.Domain;

namespace pmsa.Security;

public enum AdministrationOutcome
{
    Succeeded,
    EmailInUse,
    NotFound,
    WouldRemoveLastAdmin,
    Invalid,
}

public record AdministrationResult(AdministrationOutcome Outcome, string? Message = null, Person? Person = null)
{
    public const string EmailInUseMessage = "That email address is already in use.";
    public const string LastAdminMessage = "The system must retain at least one active Admin.";

    public bool Succeeded => Outcome is AdministrationOutcome.Succeeded;

    public static AdministrationResult Ok(Person? person = null) =>
        new(AdministrationOutcome.Succeeded, Person: person);
}

/// <summary>
/// Account administration: creating people, changing roles, deactivating and reactivating
/// (FR-013, FR-015, FR-017, FR-018).
/// </summary>
public class PeopleAdministrationService(
    PmsaDbContext db,
    IPasswordHasher hasher,
    AdminInvariantLock adminLock,
    TimeProvider clock)
{
    public Task<List<Person>> ListAsync(CancellationToken cancellationToken = default) =>
        db.People
            .AsNoTracking()
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.FullName)
            .ToListAsync(cancellationToken);

    public Task<Person?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.People.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <summary>
    /// FR-013. The password is admin-set, so the new person must replace it at first sign-in
    /// (FR-014) — that is <see cref="Person.Create"/>'s default, not something set here.
    /// </summary>
    public async Task<AdministrationResult> CreateAsync(
        string fullName, string email, Role role, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > Person.FullNameMaxLength)
        {
            return new AdministrationResult(AdministrationOutcome.Invalid, "A full name of 1–200 characters is required.");
        }

        if (!EmailAddress.IsValid(email))
        {
            return new AdministrationResult(AdministrationOutcome.Invalid, "A valid email address of at most 254 characters is required.");
        }

        if (PasswordPolicy.Validate(password) is { } passwordProblem)
        {
            return new AdministrationResult(AdministrationOutcome.Invalid, passwordProblem);
        }

        var normalisedEmail = EmailAddress.Normalise(email);

        // The unique index is the actual guarantee; this check exists to turn a violation into
        // the message SC-015 asks for rather than an exception.
        if (await db.People.AnyAsync(p => p.Email == normalisedEmail, cancellationToken))
        {
            return new AdministrationResult(AdministrationOutcome.EmailInUse, AdministrationResult.EmailInUseMessage);
        }

        var person = Person.Create(fullName, normalisedEmail, hasher.Hash(password), role, clock.GetUtcNow());
        db.People.Add(person);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index caught a duplicate created between the check above and this write.
            db.Entry(person).State = EntityState.Detached;

            if (await EmailExistsAsync(normalisedEmail, cancellationToken))
            {
                return new AdministrationResult(AdministrationOutcome.EmailInUse, AdministrationResult.EmailInUseMessage);
            }

            throw;
        }

        return AdministrationResult.Ok(person);
    }

    /// <summary>
    /// FR-017, guarded by FR-018. Demoting the last active Admin is refused; demoting yourself
    /// while other active Admins exist is allowed (EC-4).
    /// </summary>
    public Task<AdministrationResult> ChangeRoleAsync(Guid personId, Role role, CancellationToken cancellationToken = default) =>
        MutateUnderInvariantAsync(personId, person => person.ChangeRole(role), cancellationToken);

    /// <summary>FR-015, guarded by FR-018.</summary>
    public Task<AdministrationResult> DeactivateAsync(Guid personId, CancellationToken cancellationToken = default) =>
        MutateUnderInvariantAsync(personId, person => person.Deactivate(clock.GetUtcNow()), cancellationToken);

    /// <summary>
    /// FR-015. Reactivation can never violate the last-Admin invariant, but it goes through the
    /// same path so that a reactivation racing a deactivation is still serialised.
    /// </summary>
    public Task<AdministrationResult> ReactivateAsync(Guid personId, CancellationToken cancellationToken = default) =>
        MutateUnderInvariantAsync(personId, person => person.Reactivate(), cancellationToken);

    /// <summary>An Admin resetting someone's password; the person must then choose a new one (FR-014).</summary>
    public async Task<AdministrationResult> ResetPasswordAsync(
        Guid personId, string password, CancellationToken cancellationToken = default)
    {
        if (PasswordPolicy.Validate(password) is { } problem)
        {
            return new AdministrationResult(AdministrationOutcome.Invalid, problem);
        }

        var person = await db.People.SingleOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return new AdministrationResult(AdministrationOutcome.NotFound);
        }

        person.SetAdminAssignedPassword(hasher.Hash(password));
        await db.SaveChangesAsync(cancellationToken);
        return AdministrationResult.Ok(person);
    }

    /// <summary>
    /// Applies a change and refuses it if it would leave zero active Admins. The count is read
    /// <em>after</em> the change is staged, so the rule is stated once and covers deactivation,
    /// demotion, and any combination of the two.
    /// </summary>
    private async Task<AdministrationResult> MutateUnderInvariantAsync(
        Guid personId, Action<Person> mutate, CancellationToken cancellationToken)
    {
        using var _ = await adminLock.AcquireAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var person = await db.People.SingleOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return new AdministrationResult(AdministrationOutcome.NotFound);
        }

        var wasActiveAdmin = person is { IsActive: true, Role: Role.Admin };
        mutate(person);
        var isActiveAdmin = person is { IsActive: true, Role: Role.Admin };

        // Only a change that takes an active Admin away can violate the invariant. The count is
        // read from the database, so it is unaffected by the change staged above.
        var removesAnAdmin = wasActiveAdmin && !isActiveAdmin;
        var anotherActiveAdminExists = removesAnAdmin && await db.People.AnyAsync(
            p => p.Id != personId && p.Role == Role.Admin && p.IsActive, cancellationToken);

        if (removesAnAdmin && !anotherActiveAdminExists)
        {
            // Nothing is half-saved: the staged change is reverted and the transaction rolled back.
            await db.Entry(person).ReloadAsync(cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            return new AdministrationResult(AdministrationOutcome.WouldRemoveLastAdmin, AdministrationResult.LastAdminMessage);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AdministrationResult.Ok(person);
    }

    private Task<bool> EmailExistsAsync(string normalisedEmail, CancellationToken cancellationToken) =>
        db.People.AsNoTracking().AnyAsync(p => p.Email == normalisedEmail, cancellationToken);
}
