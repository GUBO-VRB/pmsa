using Microsoft.EntityFrameworkCore;
using pmsa.Domain;

namespace pmsa.Data;

public class PmsaDbContext(DbContextOptions<PmsaDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var person = modelBuilder.Entity<Person>();

        person.HasKey(p => p.Id);
        person.Property(p => p.FullName).IsRequired().HasMaxLength(Person.FullNameMaxLength);
        person.Property(p => p.PasswordHash).IsRequired();
        person.Property(p => p.Role).HasConversion<string>().IsRequired();
        person.Property(p => p.IsActive).IsRequired();
        person.Property(p => p.MustChangePassword).IsRequired();
        person.Property(p => p.FailedAttemptCount).IsRequired();
        person.Property(p => p.CreatedAt).IsRequired();

        // The email is stored already normalised (lower-cased, trimmed), so a plain unique index
        // gives the case-insensitive uniqueness the "Unique identity" invariant demands — for
        // deactivated people too, since a leaver's address must never be reused.
        person.Property(p => p.Email).IsRequired().HasMaxLength(EmailAddress.MaxLength);
        person.HasIndex(p => p.Email).IsUnique();

        // The people list and the active-Admin count are the only queries this feature makes
        // beyond a lookup by email or id.
        person.HasIndex(p => new { p.Role, p.IsActive });
    }
}
