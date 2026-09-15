using System.Net;
using Microsoft.Extensions.DependencyInjection;
using pmsa.Domain;
using pmsa.Security;
using pmsa.Tests.Infrastructure;

namespace pmsa.Tests;

/// <summary>Spec 007 acceptance scenarios SC-014 to SC-017, SC-019 and SC-020.</summary>
public class AdministrationScenarios : ScenarioTest
{
    private const string NewPassword = "a brand new passphrase";

    /// <summary>SC-014: an Admin creates a working account (FR-013).</summary>
    [Fact]
    public async Task An_admin_creates_an_account_that_can_sign_in()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        var admin = App.CreateWebClient();
        await admin.SignInSuccessfullyAsync("dee@acme.example", TestPeople.DefaultPassword);

        var response = await admin.PostFormAsync("/Admin/People/Create", new Dictionary<string, string>
        {
            ["Input.FullName"] = "Cy Nolan",
            ["Input.Email"] = "cy@acme.example",
            ["Input.Role"] = nameof(Role.User),
            ["Input.Password"] = NewPassword,
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var cy = await App.FindByEmailAsync("cy@acme.example");
        Assert.NotNull(cy);
        Assert.True(cy.IsActive);
        Assert.Equal(Role.User, cy.Role);

        // The account genuinely works — signing in reaches the forced password change (FR-014).
        var cyClient = App.CreateWebClient();
        var signIn = await cyClient.SignInAsync("cy@acme.example", NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.Contains(AuthenticationDefaults.ChangePasswordPath, signIn.Headers.Location!.OriginalString);
    }

    /// <summary>SC-015: email addresses are unique (FR-013, the "Unique identity" invariant).</summary>
    [Fact]
    public async Task A_duplicate_email_address_is_refused()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        await App.CreateAsync("ann@acme.example");

        var admin = App.CreateWebClient();
        await admin.SignInSuccessfullyAsync("dee@acme.example", TestPeople.DefaultPassword);

        var response = await admin.PostFormAsync("/Admin/People/Create", new Dictionary<string, string>
        {
            ["Input.FullName"] = "Another Ann",
            ["Input.Email"] = "ANN@acme.example",   // same address, different case
            ["Input.Role"] = nameof(Role.User),
            ["Input.Password"] = NewPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(AdministrationResult.EmailInUseMessage, (await response.ParseAsync()).Text());

        var people = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ListAsync());
        Assert.Equal(2, people.Count);
    }

    /// <summary>
    /// The same rule for a leaver: a deactivated person's address is not reusable, because
    /// rebinding it would silently reattribute their history.
    /// </summary>
    [Fact]
    public async Task A_leavers_email_address_cannot_be_reused()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        var bob = await App.CreateAsync("bob@acme.example");
        await App.DeactivateAsync(bob.Id);

        var result = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>()
                .CreateAsync("Bob Two", "bob@acme.example", Role.User, NewPassword));

        Assert.Equal(AdministrationOutcome.EmailInUse, result.Outcome);
    }

    /// <summary>SC-016: the first sign-in after an admin-set password forces a change (FR-014).</summary>
    [Fact]
    public async Task First_sign_in_blocks_everything_until_the_password_is_changed()
    {
        await App.CreateAsync("cy@acme.example", mustChangePassword: true);
        var client = App.CreateWebClient();

        var signIn = await client.SignInAsync("cy@acme.example", TestPeople.DefaultPassword);
        Assert.Contains(AuthenticationDefaults.ChangePasswordPath, signIn.Headers.Location!.OriginalString);

        // Nothing else is reachable in the meantime — including by address (FR-014, blocking).
        var elsewhere = await client.GetAsync(SomeProtectedPage);
        Assert.Equal(HttpStatusCode.Redirect, elsewhere.StatusCode);
        Assert.Contains(AuthenticationDefaults.ChangePasswordPath, elsewhere.Headers.Location!.OriginalString);

        var changed = await ChangePasswordAsync(client, TestPeople.DefaultPassword, NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);

        // The rest of the application is open now.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(SomeProtectedPage)).StatusCode);

        // And a later sign-in does not ask again — with the new password, not the old one.
        var later = App.CreateWebClient();
        var oldPassword = await later.SignInAsync("cy@acme.example", TestPeople.DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, oldPassword.StatusCode);

        var newPassword = await later.SignInAsync("cy@acme.example", NewPassword);
        Assert.Equal("/", newPassword.Headers.Location?.OriginalString);
    }

    /// <summary>EC-10: on a forced change, the new password may not equal the current one.</summary>
    [Fact]
    public async Task A_forced_change_cannot_reuse_the_same_password()
    {
        await App.CreateAsync("cy@acme.example", mustChangePassword: true);
        var client = App.CreateWebClient();
        await client.SignInAsync("cy@acme.example", TestPeople.DefaultPassword);

        var response = await ChangePasswordAsync(client, TestPeople.DefaultPassword, TestPeople.DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(PasswordChangeResult.SameAsCurrentMessage, (await response.ParseAsync()).Text());
    }

    /// <summary>EC-9: a password below the minimum length is rejected and changes nothing.</summary>
    [Fact]
    public async Task A_short_password_is_rejected_and_the_existing_one_still_works()
    {
        var ann = await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);
        var hashBefore = (await App.ReloadAsync(ann.Id))!.PasswordHash;

        var response = await ChangePasswordAsync(client, TestPeople.DefaultPassword, "short");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("at least 12 characters", (await response.ParseAsync()).Text());
        Assert.Equal(hashBefore, (await App.ReloadAsync(ann.Id))!.PasswordHash);
    }

    /// <summary>SC-020: changing your own password requires the current one (FR-019).</summary>
    [Fact]
    public async Task Changing_your_password_requires_the_current_one()
    {
        var ann = await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);
        var hashBefore = (await App.ReloadAsync(ann.Id))!.PasswordHash;

        var response = await ChangePasswordAsync(client, "not her current password", NewPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(PasswordChangeResult.CurrentPasswordIncorrectMessage, (await response.ParseAsync()).Text());
        Assert.Equal(hashBefore, (await App.ReloadAsync(ann.Id))!.PasswordHash);
    }

    /// <summary>FR-019: with the current password supplied, the change goes through.</summary>
    [Fact]
    public async Task A_person_can_change_their_own_password()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        var response = await ChangePasswordAsync(client, TestPeople.DefaultPassword, NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var fresh = App.CreateWebClient();
        Assert.Equal(HttpStatusCode.OK, (await fresh.SignInAsync("ann@acme.example", TestPeople.DefaultPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await fresh.SignInAsync("ann@acme.example", NewPassword)).StatusCode);
    }

    /// <summary>
    /// SC-017: a leaver loses access and their record — the thing every time entry points at —
    /// survives untouched (FR-015, FR-016). The "Apollo still shows 40 hours" half of the
    /// scenario needs TimeEntry and burn-down, which are stories 001 and 015.
    /// </summary>
    [Fact]
    public async Task A_leaver_loses_access_and_keeps_their_record()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        var bob = await App.CreateAsync("bob@acme.example", fullName: "Bob Ryan");

        var admin = App.CreateWebClient();
        await admin.SignInSuccessfullyAsync("dee@acme.example", TestPeople.DefaultPassword);

        var response = await admin.PostFormAsync(
            $"/Admin/People?handler=Deactivate&id={bob.Id}",
            new Dictionary<string, string>(),
            formUrl: "/Admin/People");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var reloaded = await App.ReloadAsync(bob.Id);
        Assert.NotNull(reloaded);                       // never deleted
        Assert.False(reloaded.IsActive);
        Assert.NotNull(reloaded.DeactivatedAt);
        Assert.Equal("Bob Ryan", reloaded.FullName);    // still nameable in a report

        var bobClient = App.CreateWebClient();
        Assert.Equal(HttpStatusCode.OK, (await bobClient.SignInAsync("bob@acme.example", TestPeople.DefaultPassword)).StatusCode);
    }

    /// <summary>EC-14: a reactivated person signs in with their previous password.</summary>
    [Fact]
    public async Task A_reactivated_person_signs_in_with_their_previous_password()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        var bob = await App.CreateAsync("bob@acme.example");
        await App.DeactivateAsync(bob.Id);

        await App.WithScopeAsync(async services =>
        {
            var result = await services.GetRequiredService<PeopleAdministrationService>().ReactivateAsync(bob.Id);
            Assert.True(result.Succeeded, result.Message);
        });

        var reloaded = await App.ReloadAsync(bob.Id);
        Assert.True(reloaded!.IsActive);
        Assert.Null(reloaded.DeactivatedAt);

        var client = App.CreateWebClient();
        Assert.Equal(HttpStatusCode.Redirect, (await client.SignInAsync("bob@acme.example", TestPeople.DefaultPassword)).StatusCode);
    }

    /// <summary>SC-019: the last active Admin cannot deactivate or demote themselves (FR-018).</summary>
    [Fact]
    public async Task The_last_admin_cannot_be_removed()
    {
        var dee = await App.CreateAsync("dee@acme.example", Role.Admin);
        await App.CreateAsync("ann@acme.example", Role.User);
        await App.CreateAsync("mo@acme.example", Role.Manager);

        var admin = App.CreateWebClient();
        await admin.SignInSuccessfullyAsync("dee@acme.example", TestPeople.DefaultPassword);

        var deactivation = await admin.PostFormAsync(
            $"/Admin/People?handler=Deactivate&id={dee.Id}",
            new Dictionary<string, string>(),
            formUrl: "/Admin/People");
        Assert.Equal(HttpStatusCode.Redirect, deactivation.StatusCode);
        Assert.True((await App.ReloadAsync(dee.Id))!.IsActive);

        var demotion = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ChangeRoleAsync(dee.Id, Role.User));
        Assert.Equal(AdministrationOutcome.WouldRemoveLastAdmin, demotion.Outcome);
        Assert.Equal(AdministrationResult.LastAdminMessage, demotion.Message);
        Assert.Equal(Role.Admin, (await App.ReloadAsync(dee.Id))!.Role);

        // The refusal is reported, not swallowed.
        var list = await admin.GetDocumentAsync("/Admin/People");
        Assert.Contains(AdministrationResult.LastAdminMessage, list.Text());
    }

    /// <summary>EC-4: an Admin may demote themselves while another active Admin exists.</summary>
    [Fact]
    public async Task An_admin_may_demote_themselves_when_another_admin_remains()
    {
        var dee = await App.CreateAsync("dee@acme.example", Role.Admin);
        await App.CreateAsync("eve@acme.example", Role.Admin);

        var result = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ChangeRoleAsync(dee.Id, Role.User));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(Role.User, (await App.ReloadAsync(dee.Id))!.Role);
    }

    /// <summary>EC-5: two Admins deactivating each other at once cannot leave zero active Admins.</summary>
    [Fact]
    public async Task Concurrent_deactivations_cannot_leave_the_system_without_an_admin()
    {
        var dee = await App.CreateAsync("dee@acme.example", Role.Admin);
        var eve = await App.CreateAsync("eve@acme.example", Role.Admin);

        // Separate scopes, as two simultaneous requests would have.
        var both = await Task.WhenAll(
            App.WithScopeAsync(services =>
                services.GetRequiredService<PeopleAdministrationService>().DeactivateAsync(eve.Id)),
            App.WithScopeAsync(services =>
                services.GetRequiredService<PeopleAdministrationService>().DeactivateAsync(dee.Id)));

        Assert.Equal(1, both.Count(r => r.Succeeded));
        Assert.Equal(1, both.Count(r => r.Outcome is AdministrationOutcome.WouldRemoveLastAdmin));

        var survivors = await App.WithScopeAsync(services =>
            services.GetRequiredService<PeopleAdministrationService>().ListAsync());
        Assert.Single(survivors, p => p is { IsActive: true, Role: Role.Admin });
    }

    /// <summary>EC-11: an over-length name is refused with the limit named.</summary>
    [Fact]
    public async Task An_over_length_full_name_is_refused()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        var admin = App.CreateWebClient();
        await admin.SignInSuccessfullyAsync("dee@acme.example", TestPeople.DefaultPassword);

        var response = await admin.PostFormAsync("/Admin/People/Create", new Dictionary<string, string>
        {
            ["Input.FullName"] = new string('x', Person.FullNameMaxLength + 1),
            ["Input.Email"] = "long@acme.example",
            ["Input.Role"] = nameof(Role.User),
            ["Input.Password"] = NewPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("200 characters", (await response.ParseAsync()).Text());
        Assert.Null(await App.FindByEmailAsync("long@acme.example"));
    }

    private static Task<HttpResponseMessage> ChangePasswordAsync(
        HttpClient client, string current, string replacement) =>
        client.PostFormAsync(AuthenticationDefaults.ChangePasswordPath, new Dictionary<string, string>
        {
            ["Input.CurrentPassword"] = current,
            ["Input.NewPassword"] = replacement,
            ["Input.ConfirmPassword"] = replacement,
        });
}
