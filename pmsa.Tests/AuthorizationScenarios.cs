using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using pmsa.Domain;
using pmsa.Security;
using pmsa.Tests.Infrastructure;

namespace pmsa.Tests;

/// <summary>Spec 007 acceptance scenarios SC-010 to SC-013 and SC-018.</summary>
public class AuthorizationScenarios : ScenarioTest
{
    /// <summary>SC-010: a User is not shown capabilities above their role (FR-009).</summary>
    [Fact]
    public async Task A_user_sees_no_administration_navigation()
    {
        await App.CreateAsync("ann@acme.example", Role.User);
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        var page = await client.GetDocumentAsync("/Index");

        Assert.Null(page.QuerySelector("a[href='/Admin/People']"));
        Assert.Null(page.QuerySelector("a[href^='/Admin']"));
    }

    /// <summary>The same page, seen by an Admin, does offer it — otherwise SC-010 proves nothing.</summary>
    [Fact]
    public async Task An_admin_is_shown_the_people_navigation()
    {
        await App.CreateAsync("dee@acme.example", Role.Admin);
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("dee@acme.example", TestPeople.DefaultPassword);

        var page = await client.GetDocumentAsync("/Index");

        Assert.NotNull(page.QuerySelector("a[href^='/Admin/People']"));
    }

    /// <summary>SC-011: a capability reached directly is refused, and discloses nothing (FR-010).</summary>
    [Theory]
    [InlineData(Role.User)]
    [InlineData(Role.Manager)]
    public async Task Administration_addressed_directly_is_refused_below_the_admin_role(Role role)
    {
        await App.CreateAsync("ann@acme.example", role);
        await App.CreateAsync("secret@acme.example", Role.Manager, fullName: "Confidential Colleague");

        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        var refused = await client.GetAsync("/Admin/People");

        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.Contains(AuthenticationDefaults.ForbiddenPath, refused.Headers.Location!.OriginalString);

        // Following the refusal must produce a 403 and none of the data behind it.
        var forbidden = await client.GetAsync(refused.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.DoesNotContain("Confidential Colleague", (await forbidden.ParseAsync()).Text());
    }

    /// <summary>SC-011, the write side: a POST to an administration handler is refused too.</summary>
    [Fact]
    public async Task A_user_cannot_post_to_an_administration_handler()
    {
        var bob = await App.CreateAsync("bob@acme.example");
        await App.CreateAsync("ann@acme.example", Role.User);

        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        var response = await client.PostAsync(
            $"/Admin/People?handler=Deactivate&id={bob.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AuthenticationDefaults.ForbiddenPath, response.Headers.Location!.OriginalString);
        Assert.True((await App.ReloadAsync(bob.Id))!.IsActive);
    }

    /// <summary>
    /// SC-012 and SC-013, to the extent this feature can carry them. Other people's entries and
    /// per-person reports belong to stories 011 and 012–016, so what is asserted here is the rule
    /// those stories will sit behind: the Manager capability admits Manager and Admin, and
    /// refuses User (FR-011, FR-012).
    /// </summary>
    [Theory]
    [InlineData(Role.User, false)]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Admin, true)]
    public async Task The_manager_capability_admits_exactly_manager_and_admin(Role role, bool expected)
    {
        var person = await App.CreateAsync($"{role}@acme.example".ToLowerInvariant(), role);

        var granted = await App.WithScopeAsync(async services =>
        {
            var authorization = services.GetRequiredService<IAuthorizationService>();
            var principal = PersonClaims.CreatePrincipal(person, AuthenticationDefaults.Scheme);
            var result = await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.RequireManager);
            return result.Succeeded;
        });

        Assert.Equal(expected, granted);
    }

    /// <summary>SC-018: a role change takes effect at the next request (FR-017).</summary>
    [Fact]
    public async Task A_role_change_takes_effect_on_the_next_request()
    {
        var mo = await App.CreateAsync("mo@acme.example", Role.Admin);
        await App.CreateAsync("dee@acme.example", Role.Admin); // keeps FR-018 satisfied

        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("mo@acme.example", TestPeople.DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/People")).StatusCode);

        await App.ChangeRoleAsync(mo.Id, Role.User);

        // Same cookie, same client — no re-authentication anywhere in between.
        var refused = await client.GetAsync("/Admin/People");
        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.Contains(AuthenticationDefaults.ForbiddenPath, refused.Headers.Location!.OriginalString);
    }

    /// <summary>
    /// SC-006, to the extent this feature can carry it. TimeEntry belongs to spec 001, so what is
    /// asserted here is FR-005's precondition: the identity the application acts on comes from
    /// the session and cannot be influenced by anything in the request.
    /// </summary>
    [Fact]
    public async Task The_acting_identity_comes_from_the_session_and_not_from_the_request()
    {
        var ann = await App.CreateAsync("ann@acme.example", fullName: "Ann Devlin");
        await App.CreateAsync("bob@acme.example", fullName: "Bob Ryan");

        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        // Every lever a request could plausibly pull at the owner of an action.
        var page = await client.GetDocumentAsync(
            $"/Index?personId={Guid.NewGuid()}&userId=bob@acme.example&Input.Email=bob@acme.example");

        Assert.Contains("Ann Devlin", page.Text());
        Assert.DoesNotContain("Bob Ryan", page.Text());
        Assert.Equal(ann.Id, (await App.FindByEmailAsync("ann@acme.example"))!.Id);
    }
}
