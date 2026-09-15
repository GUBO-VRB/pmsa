using Microsoft.EntityFrameworkCore;
using pmsa.Domain;

namespace pmsa.Data;

public class PmsaDbContext(DbContextOptions<PmsaDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePerson(modelBuilder);
        ConfigureProject(modelBuilder);
        ConfigureTimeEntry(modelBuilder);
    }

    private static void ConfigurePerson(ModelBuilder modelBuilder)
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

    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<Project>();

        project.HasKey(p => p.Id);
        project.Property(p => p.Name).IsRequired().HasMaxLength(Project.NameMaxLength);
        project.Property(p => p.Client).HasMaxLength(Project.ClientMaxLength);
        project.Property(p => p.IsActive).IsRequired();
        project.Property(p => p.CreatedAt).IsRequired();

        // Not unique: spec 012, EC-14 requires two projects sharing a name to stay two slices.
        project.HasIndex(p => p.Name);
    }

    private static void ConfigureTimeEntry(ModelBuilder modelBuilder)
    {
        var entry = modelBuilder.Entity<TimeEntry>();

        entry.HasKey(e => e.Id);
        entry.Property(e => e.PersonId).IsRequired();
        entry.Property(e => e.ProjectId).IsRequired();
        entry.Property(e => e.Date).IsRequired();
        entry.Property(e => e.BeginMinutes).IsRequired();
        entry.Property(e => e.EndMinutes).IsRequired();
        entry.Property(e => e.CreatedAt).IsRequired();

        entry.Ignore(e => e.DurationMinutes);

        // Restrict, not Cascade: a Person is never deleted (spec 007) and a Project is archived
        // rather than removed (spec 012, FR-017). A cascade here would be a path to the silent
        // history loss both invariants exist to prevent.
        entry.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Restrict);
        entry.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

        // Spec 012, NFR-003: the report must resolve (date-range, person, project) without
        // scanning ~1.5 M rows. Two indexes, one per scoping shape:
        //   - a Manager reports across everyone, so the range leads and the project follows,
        //     which lets the group-by read the index rather than the table;
        //   - a User is pinned to their own entries (FR-013), so the person leads.
        entry.HasIndex(e => new { e.Date, e.ProjectId });
        entry.HasIndex(e => new { e.PersonId, e.Date, e.ProjectId });
    }
}
