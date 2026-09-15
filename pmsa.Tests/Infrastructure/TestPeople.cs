using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pmsa.Data;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Tests.Infrastructure;

/// <summary>
/// Puts people into the store for a test to act on.
/// </summary>
/// <remarks>
/// Deliberately goes through <see cref="Person"/>'s own methods rather than raw SQL, so a test
/// subject can never be in a state the domain would not allow.
/// </remarks>
public static class TestPeople
{
    public const string DefaultPassword = "correct horse battery";

    /// <summary>
    /// Creates a person who can sign in straight away. <see cref="Person.Create"/> always sets
    /// the forced-change flag (FR-014), so it is cleared here by setting the password as the
    /// person themselves would.
    /// </summary>
    public static Task<Person> CreateAsync(
        this PmsaApplicationFactory factory,
        string email,
        Role role = Role.User,
        string password = DefaultPassword,
        string? fullName = null,
        bool isActive = true,
        bool mustChangePassword = false) =>
        factory.WithScopeAsync(async services =>
        {
            var db = services.GetRequiredService<PmsaDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var clock = services.GetRequiredService<TimeProvider>();

            var person = Person.Create(
                fullName ?? email.Split('@')[0],
                email,
                hasher.Hash(password),
                role,
                clock.GetUtcNow());

            if (!mustChangePassword)
            {
                person.SetPassword(hasher.Hash(password));
            }

            if (!isActive)
            {
                person.Deactivate(clock.GetUtcNow());
            }

            db.People.Add(person);
            await db.SaveChangesAsync();
            return person;
        });

    public static Task<Person?> ReloadAsync(this PmsaApplicationFactory factory, Guid id) =>
        factory.WithScopeAsync(services => services
            .GetRequiredService<PmsaDbContext>()
            .People.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id));

    public static Task<Person?> FindByEmailAsync(this PmsaApplicationFactory factory, string email)
    {
        var normalised = EmailAddress.Normalise(email);
        return factory.WithScopeAsync(services => services
            .GetRequiredService<PmsaDbContext>()
            .People.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Email == normalised));
    }

    public static Task DeactivateAsync(this PmsaApplicationFactory factory, Guid id) =>
        factory.WithScopeAsync(async services =>
        {
            var result = await services.GetRequiredService<PeopleAdministrationService>().DeactivateAsync(id);
            Assert.True(result.Succeeded, result.Message);
        });

    public static Task ChangeRoleAsync(this PmsaApplicationFactory factory, Guid id, Role role) =>
        factory.WithScopeAsync(async services =>
        {
            var result = await services.GetRequiredService<PeopleAdministrationService>().ChangeRoleAsync(id, role);
            Assert.True(result.Succeeded, result.Message);
        });
}
