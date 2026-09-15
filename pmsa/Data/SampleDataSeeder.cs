using Microsoft.EntityFrameworkCore;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Data;

/// <summary>Switches on the demo data below. Off unless a configuration file turns it on.</summary>
public class SampleDataOptions
{
    public const string SectionName = "SampleData";

    public bool Enabled { get; set; }

    /// <summary>The password every sample person is given. They must replace it at first sign-in.</summary>
    public string Password { get; set; } = "sample-password-01";
}

/// <summary>
/// Demo projects, people and time entries, so that the report of spec 012 has something to report
/// on before stories 001 and 008 build the screens that create them for real.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Not part of spec 012.</strong> This is scaffolding, and it is expected to be deleted
/// once time entry (spec 001) and projects (story 008) exist.
/// </para>
/// <para>
/// It runs only when <see cref="SampleDataOptions.Enabled"/> is set, only in Development, and only
/// while the time-entry table is empty, so it cannot touch data anyone has typed. The generated
/// entries are deterministic — a fixed seed, no clock beyond the supplied date — so two runs
/// against the same day produce the same report.
/// </para>
/// </remarks>
public class SampleDataSeeder(
    PmsaDbContext db,
    IPasswordHasher hasher,
    TimeProvider clock,
    ILogger<SampleDataSeeder> logger)
{
    // Eighteen, so that a report over them exceeds the fifteen bars FR-012 charts and the
    // "Other (N projects)" tail is visible on the demo data rather than only in theory.
    private static readonly (string Name, string Client)[] SampleProjects =
    [
        ("Apollo", "Northwind"), ("Atlas", "Northwind"), ("Zeus", "Contoso"),
        ("Hermes", "Contoso"), ("Orion", "Fabrikam"), ("Vesta", "Fabrikam"),
        ("Juno", "Tailspin"), ("Ceres", "Tailspin"), ("Pallas", "Tailspin"),
        ("Iris", "Northwind"), ("Rhea", "Contoso"), ("Theia", "Fabrikam"),
        ("Helios", "Northwind"), ("Selene", "Contoso"), ("Eos", "Fabrikam"),
        ("Nyx", "Tailspin"), ("Hydra", "Contoso"), ("Internal", "pmsa"),
    ];

    private static readonly (string Name, string Email, Role Role)[] SamplePeople =
    [
        ("Ann Devers", "ann@localhost.example", Role.User),
        ("Bob Carrow", "bob@localhost.example", Role.User),
        ("Cleo Nunes", "cleo@localhost.example", Role.User),
        ("Mo Farrow", "mo@localhost.example", Role.Manager),
    ];

    public async Task SeedAsync(SampleDataOptions options, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (await db.TimeEntries.AnyAsync(cancellationToken))
        {
            logger.LogDebug("Sample data already present; leaving it untouched.");
            return;
        }

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

        var projects = await EnsureProjectsAsync(now, cancellationToken);
        var people = await EnsurePeopleAsync(options, now, cancellationToken);

        // Archived after the entries are generated below, so that the report still has to show
        // them (FR-017, SC-013).
        projects[^1].Archive();

        db.TimeEntries.AddRange(GenerateEntries(people, projects, today, now));
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded sample data: {Projects} projects, {People} people, and time entries for the last 120 days. " +
            "Every sample person signs in with the configured {Section}:Password and must then change it.",
            projects.Count, people.Count, SampleDataOptions.SectionName);
    }

    private async Task<List<Project>> EnsureProjectsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await db.Projects.ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            return existing;
        }

        var created = SampleProjects.Select(p => Project.Create(p.Name, p.Client, now)).ToList();
        db.Projects.AddRange(created);
        return created;
    }

    private async Task<List<Person>> EnsurePeopleAsync(
        SampleDataOptions options, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var people = new List<Person>();
        var hash = hasher.Hash(options.Password);

        foreach (var (name, email, role) in SamplePeople)
        {
            var normalised = EmailAddress.Normalise(email);
            var person = await db.People.SingleOrDefaultAsync(p => p.Email == normalised, cancellationToken);

            if (person is null)
            {
                person = Person.Create(name, normalised, hash, role, now);
                db.People.Add(person);
            }

            people.Add(person);
        }

        // A leaver whose hours must keep counting (FR-017, SC-012). Deactivated directly rather
        // than through PeopleAdministrationService, which guards an invariant about Admins that
        // has nothing to say about sample Users.
        people[1].Deactivate(now);

        return people;
    }

    /// <summary>
    /// Four months of plausible days. Per person and date the blocks are laid end to end, so the
    /// <em>no hour is booked twice</em> invariant holds without needing an overlap check.
    /// </summary>
    private static IEnumerable<TimeEntry> GenerateEntries(
        List<Person> people, List<Project> projects, DateOnly today, DateTimeOffset now)
    {
        var random = new Random(Seed: 20260915);

        for (var offset = 120; offset >= 0; offset--)
        {
            var date = today.AddDays(-offset);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            foreach (var person in people)
            {
                if (random.Next(10) == 0)
                {
                    continue; // A day off.
                }

                var minute = 9 * 60;
                var blocks = random.Next(1, 4);

                for (var block = 0; block < blocks; block++)
                {
                    var length = random.Next(2, 7) * 30;
                    if (minute + length > 18 * 60)
                    {
                        break;
                    }

                    // Weighted towards the first few projects, so the report has a shape and a
                    // tail rather than nine equal bars.
                    var project = projects[Math.Min(projects.Count - 1, random.Next(projects.Count) * random.Next(1, 3) / 2)];

                    yield return TimeEntry.Create(person.Id, project.Id, date, minute, minute + length, now);

                    minute += length + 30; // A gap, so no two blocks touch.
                }
            }
        }
    }
}
