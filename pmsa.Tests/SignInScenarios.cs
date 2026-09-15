using System.Net;
using pmsa.Domain;
using pmsa.Security;
using pmsa.Tests.Infrastructure;

namespace pmsa.Tests;

/// <summary>Spec 007 acceptance scenarios SC-001 to SC-004 and SC-009.</summary>
public class SignInScenarios : ScenarioTest
{
    /// <summary>SC-001: successful sign-in (FR-001).</summary>
    [Fact]
    public async Task Successful_sign_in_lands_on_the_day_screen_and_names_the_person()
    {
        await App.CreateAsync("ann@acme.example", fullName: "Ann Devlin");
        var client = App.CreateWebClient();

        var response = await client.SignInAsync("ann@acme.example", TestPeople.DefaultPassword);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);

        var landing = await client.GetDocumentAsync("/Index");
        Assert.Contains("Ann Devlin", landing.Text());
    }

    /// <summary>SC-002: a wrong password is refused with the non-specific message (FR-002).</summary>
    [Fact]
    public async Task Wrong_password_is_refused_without_saying_why()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        var response = await client.SignInAsync("ann@acme.example", "not her password");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(SignInAttemptResult.RefusalMessage, (await response.ParseAsync()).Text());
        Assert.False(IssuedASession(response));
    }

    /// <summary>SC-003: an unknown email is indistinguishable from a wrong password (FR-002).</summary>
    [Fact]
    public async Task Unknown_email_gives_exactly_the_same_message_as_a_wrong_password()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        var unknownEmail = await (await client.SignInAsync("nobody@acme.example", "any password at all")).ParseAsync();
        var wrongPassword = await (await client.SignInAsync("ann@acme.example", "any password at all")).ParseAsync();

        Assert.Contains(SignInAttemptResult.RefusalMessage, unknownEmail.Text());
        Assert.Contains(SignInAttemptResult.RefusalMessage, wrongPassword.Text());
    }

    /// <summary>SC-004: a deactivated account cannot sign in, with the same message (FR-003).</summary>
    [Fact]
    public async Task Deactivated_account_cannot_sign_in()
    {
        await App.CreateAsync("bob@acme.example", isActive: false);
        var client = App.CreateWebClient();

        var response = await client.SignInAsync("bob@acme.example", TestPeople.DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(SignInAttemptResult.RefusalMessage, (await response.ParseAsync()).Text());
        Assert.False(IssuedASession(response));
    }

    /// <summary>SC-009: five failures lock the account for fifteen minutes (FR-008).</summary>
    [Fact]
    public async Task Five_consecutive_failures_lock_the_account_for_fifteen_minutes()
    {
        var ann = await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        for (var attempt = 1; attempt <= Person.MaxFailedAttempts; attempt++)
        {
            var failure = await client.SignInAsync("ann@acme.example", $"wrong {attempt}");
            Assert.Equal(HttpStatusCode.OK, failure.StatusCode);
        }

        // The 6th attempt is refused even though the password is right.
        var duringLockout = await client.SignInAsync("ann@acme.example", TestPeople.DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, duringLockout.StatusCode);
        Assert.Contains(SignInAttemptResult.RefusalMessage, (await duringLockout.ParseAsync()).Text());

        App.Clock.Advance(Person.LockoutDuration + TimeSpan.FromMinutes(1));

        var afterLockout = await client.SignInAsync("ann@acme.example", TestPeople.DefaultPassword);
        Assert.Equal(HttpStatusCode.Redirect, afterLockout.StatusCode);
        Assert.Equal("/", afterLockout.Headers.Location?.OriginalString);

        var reloaded = await App.ReloadAsync(ann.Id);
        Assert.Equal(0, reloaded!.FailedAttemptCount);
        Assert.Null(reloaded.LockedUntil);
    }

    /// <summary>EC-7: an attempt during a lockout does not extend the lockout window.</summary>
    [Fact]
    public async Task Attempts_during_a_lockout_do_not_extend_it()
    {
        var ann = await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        for (var attempt = 1; attempt <= Person.MaxFailedAttempts; attempt++)
        {
            await client.SignInAsync("ann@acme.example", $"wrong {attempt}");
        }

        var lockedUntil = (await App.ReloadAsync(ann.Id))!.LockedUntil;
        Assert.NotNull(lockedUntil);

        App.Clock.Advance(TimeSpan.FromMinutes(10));
        await client.SignInAsync("ann@acme.example", "still wrong");

        Assert.Equal(lockedUntil, (await App.ReloadAsync(ann.Id))!.LockedUntil);
    }

    /// <summary>EC-1: an empty field is validated, not authenticated — the counter does not move.</summary>
    [Fact]
    public async Task An_empty_password_is_a_validation_failure_and_does_not_count_towards_lockout()
    {
        var ann = await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        var response = await client.SignInAsync("ann@acme.example", string.Empty);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Password is required", (await response.ParseAsync()).Text());
        Assert.Equal(0, (await App.ReloadAsync(ann.Id))!.FailedAttemptCount);
    }

    /// <summary>EC-2: padding and mixed case are normalised away.</summary>
    [Fact]
    public async Task Email_is_trimmed_and_case_insensitive()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        var response = await client.SignInAsync("  Ann@Acme.Example  ", TestPeople.DefaultPassword);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    /// <summary>NFR-002: the session cookie is HttpOnly, Secure and SameSite=Lax.</summary>
    [Fact]
    public async Task The_session_cookie_is_httponly_secure_and_samesite_lax()
    {
        await App.CreateAsync("ann@acme.example");
        var client = App.CreateWebClient();

        var response = await client.SignInAsync("ann@acme.example", TestPeople.DefaultPassword);

        var setCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(AuthenticationDefaults.CookieName));

        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        // FR-007: non-persistent, so it dies with the browser. A session cookie carries no
        // Expires and no Max-Age.
        Assert.DoesNotContain("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when the response handed out a session cookie — a refusal must not.</summary>
    private static bool IssuedASession(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies) &&
        cookies.Any(c => c.StartsWith(AuthenticationDefaults.CookieName + "=", StringComparison.Ordinal) &&
                         !c.Contains(AuthenticationDefaults.CookieName + "=;", StringComparison.Ordinal));
}
