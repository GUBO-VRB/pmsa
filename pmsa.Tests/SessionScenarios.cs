using System.Net;
using pmsa.Domain;
using pmsa.Security;
using pmsa.Tests.Infrastructure;

namespace pmsa.Tests;

/// <summary>Spec 007 acceptance scenarios SC-005, SC-007, SC-008 and SC-021.</summary>
public class SessionScenarios : ScenarioTest
{
    /// <summary>SC-005: an unauthenticated visitor is redirected and then returned (FR-004, NFR-008).</summary>
    [Fact]
    public async Task Unauthenticated_visitor_is_sent_to_sign_in_and_returned_afterwards()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        var blocked = await client.GetAsync(SomeProtectedPage);

        Assert.True(blocked.RedirectsToSignIn());
        var location = blocked.Headers.Location!.OriginalString;
        Assert.Contains($"returnUrl={Uri.EscapeDataString(SomeProtectedPage)}", location);

        var signedIn = await client.SignInAsync("ann@acme.example", TestPeople.DefaultPassword, SomeProtectedPage);

        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        Assert.Equal(SomeProtectedPage, signedIn.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(SomeProtectedPage)).StatusCode);
    }

    /// <summary>
    /// SUC-002, generalised from SC-005: no page other than sign-in is reachable without a
    /// session. The error page is exempt because an unhandled error can happen to anyone.
    /// </summary>
    [Theory]
    [InlineData("/")]
    [InlineData("/Index")]
    [InlineData("/Privacy")]
    [InlineData("/Account/ChangePassword")]
    [InlineData("/Account/Forbidden")]
    [InlineData("/Admin/People")]
    [InlineData("/Admin/People/Create")]
    public async Task Every_page_but_sign_in_requires_a_session(string path)
    {
        var client = App.CreateWebClient();

        Assert.True((await client.GetAsync(path)).RedirectsToSignIn(), $"{path} was reachable without signing in.");
    }

    /// <summary>SC-007: signing out ends the session, and Back does not restore it (FR-006).</summary>
    [Fact]
    public async Task Signing_out_ends_the_session()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        var signedOut = await client.PostFormAsync("/Account/SignOut", new Dictionary<string, string>(), formUrl: "/Index");
        Assert.Equal(HttpStatusCode.Redirect, signedOut.StatusCode);

        // "Navigating back" is a fresh request for the previous page once the cookie is gone.
        Assert.True((await client.GetAsync(SomeProtectedPage)).RedirectsToSignIn());
    }

    /// <summary>NFR-005: authenticated pages are not cached, so Back cannot show them offline.</summary>
    [Fact]
    public async Task Pages_are_served_no_store()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        var response = await client.GetAsync("/Index");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty);
    }

    /// <summary>SC-008: the session expires 12 hours after sign-in and is not extended (FR-007).</summary>
    [Fact]
    public async Task Session_expires_twelve_hours_after_sign_in()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        // Activity throughout the day must not push the expiry out (SlidingExpiration is off).
        App.Clock.Advance(TimeSpan.FromHours(11));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Index")).StatusCode);

        App.Clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        Assert.True((await client.GetAsync("/Index")).RedirectsToSignIn());
    }

    /// <summary>SC-021: deactivation ends a live session at the next request (FR-021).</summary>
    [Fact]
    public async Task Deactivating_someone_ends_their_live_session()
    {
        var bob = await App.CreateAsync("bob@acme.example");
        await App.CreateAsync("dee@acme.example", Role.Admin);

        var client = App.CreateWebClient();
        await client.SignInSuccessfullyAsync("bob@acme.example", TestPeople.DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Index")).StatusCode);

        await App.DeactivateAsync(bob.Id);

        // The very next request, with the same cookie, is refused.
        Assert.True((await client.GetAsync("/Index")).RedirectsToSignIn());

        // And he cannot sign in again.
        var retry = await client.SignInAsync("bob@acme.example", TestPeople.DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Contains(SignInAttemptResult.RefusalMessage, (await retry.ParseAsync()).Text());
    }

    /// <summary>EC-8: sessions on two devices are independent.</summary>
    [Fact]
    public async Task Signing_out_on_one_device_leaves_the_other_signed_in()
    {
        await App.CreateAsync("ann@acme.example");
        var laptop = App.CreateWebClient();
        var phone = App.CreateWebClient();

        await laptop.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);
        await phone.SignInSuccessfullyAsync("ann@acme.example", TestPeople.DefaultPassword);

        await laptop.PostFormAsync("/Account/SignOut", new Dictionary<string, string>(), formUrl: "/Index");

        Assert.True((await laptop.GetAsync("/Index")).RedirectsToSignIn());
        Assert.Equal(HttpStatusCode.OK, (await phone.GetAsync("/Index")).StatusCode);
    }
}
