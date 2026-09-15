using System.Net;
using Microsoft.Extensions.DependencyInjection;
using pmsa.Data;
using pmsa.Domain;
using pmsa.Security;
using pmsa.Tests.Infrastructure;

namespace pmsa.Tests;

/// <summary>FR-020, NFR-007 and EC-13 — the seeded Admin account.</summary>
public class SeedingScenarios : ScenarioTest
{
    private const string SeedEmail = "root@acme.example";
    private const string SeedPassword = "the seeded password";

    public SeedingScenarios()
        : base(new PmsaApplicationFactory { SeedAdminEmail = SeedEmail, SeedAdminPassword = SeedPassword })
    {
    }

    /// <summary>FR-020: a deployment with no accounts yet gets exactly one, and it is an Admin.</summary>
    [Fact]
    public async Task The_first_deployment_gets_an_admin_account()
    {
        var people = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ListAsync());

        var seeded = Assert.Single(people);
        Assert.Equal(SeedEmail, seeded.Email);
        Assert.Equal(Role.Admin, seeded.Role);
        Assert.True(seeded.IsActive);
    }

    /// <summary>EC-13: the seeded password works once and is then spent (FR-014).</summary>
    [Fact]
    public async Task The_seeded_password_only_gets_as_far_as_the_password_change()
    {
        var client = App.CreateWebClient();

        var signIn = await client.SignInAsync(SeedEmail, SeedPassword);
        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.Contains(AuthenticationDefaults.ChangePasswordPath, signIn.Headers.Location!.OriginalString);

        var changed = await client.PostFormAsync(AuthenticationDefaults.ChangePasswordPath,
            new Dictionary<string, string>
            {
                ["Input.CurrentPassword"] = SeedPassword,
                ["Input.NewPassword"] = "a password only they know",
                ["Input.ConfirmPassword"] = "a password only they know",
            });
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);

        var fresh = App.CreateWebClient();
        Assert.Equal(HttpStatusCode.OK, (await fresh.SignInAsync(SeedEmail, SeedPassword)).StatusCode);
    }

    /// <summary>
    /// NFR-007: re-running the seeder — which is what a redeploy does — never resets an existing
    /// Admin's password and never reactivates one that was deactivated.
    /// </summary>
    [Fact]
    public async Task Reseeding_touches_nothing()
    {
        var seeded = (await App.FindByEmailAsync(SeedEmail))!;

        // A second Admin first, so deactivating the seeded one does not trip FR-018.
        await App.CreateAsync("dee@acme.example", Role.Admin);
        await App.DeactivateAsync(seeded.Id);

        await App.WithScopeAsync(services => services
            .GetRequiredService<AdminSeeder>()
            .SeedAsync(new SeedAdminOptions { Email = SeedEmail, Password = "a completely different password" }));

        var after = (await App.ReloadAsync(seeded.Id))!;
        Assert.False(after.IsActive);                       // not reactivated
        Assert.Equal(seeded.PasswordHash, after.PasswordHash); // not reset

        var people = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ListAsync());
        Assert.Equal(2, people.Count);                      // not duplicated
    }
}

/// <summary>A deployment that configures no seed credentials must still start.</summary>
public class UnseededDeployment : ScenarioTest
{
    [Fact]
    public async Task Starts_with_no_accounts_and_refuses_everyone()
    {
        var people = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ListAsync());
        Assert.Empty(people);

        var client = App.CreateWebClient();
        var response = await client.SignInAsync("anyone@acme.example", "any password at all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(SignInAttemptResult.RefusalMessage, (await response.ParseAsync()).Text());
    }
}
