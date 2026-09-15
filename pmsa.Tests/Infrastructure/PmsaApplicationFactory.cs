using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using pmsa.Data;

namespace pmsa.Tests.Infrastructure;

/// <summary>
/// Hosts the real application against a private in-memory SQLite database and a controllable
/// clock.
/// </summary>
/// <remarks>
/// Nothing about authentication is stubbed: the tests drive the same pages, the same cookie
/// handler and the same database schema a deployment would. The only substitutions are the
/// storage location, the clock (so FR-007's 12 hours and FR-008's 15 minutes can be reached in a
/// test) and the hashing cost (which is about the format, not the expense).
/// </remarks>
public class PmsaApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    /// <summary>Starts at a fixed instant so that nothing in a test depends on the wall clock.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero));

    /// <summary>Set before the first request to exercise the seeded-Admin path (FR-020, NFR-007).</summary>
    public string? SeedAdminEmail { get; init; }

    public string? SeedAdminPassword { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // PBKDF2 at 600,000 iterations is the production setting; the tests are about behaviour,
        // not cost, and would otherwise spend minutes deriving keys.
        builder.UseSetting("PasswordHashing:Iterations", "1000");

        builder.UseSetting("SeedAdmin:Email", SeedAdminEmail ?? string.Empty);
        builder.UseSetting("SeedAdmin:Password", SeedAdminPassword ?? string.Empty);
        builder.UseSetting("SeedAdmin:FullName", "Seeded Administrator");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PmsaDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<PmsaDbContext>();

            // A single connection held open for the factory's lifetime: closing the last
            // connection to an in-memory SQLite database discards it.
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<PmsaDbContext>(options => options.UseSqlite(_connection));

            // Registered last, so it wins over Program.cs's TimeProvider.System. The cookie
            // handler reads the same instance (see the Configure<TimeProvider> call in Program).
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>
    /// A client that keeps cookies and does not follow redirects, so a test can assert on the
    /// redirect itself. The base address is https so the <c>Secure</c> session cookie (NFR-002)
    /// is actually stored.
    /// </summary>
    public HttpClient CreateWebClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost"),
    });

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public async Task WithScopeAsync(Func<IServiceProvider, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
