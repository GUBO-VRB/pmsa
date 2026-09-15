using Microsoft.Extensions.Options;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Tests;

/// <summary>
/// The rules of Section 5.4 and the value objects of Section 5.3, tested without a web host
/// because none of them need one.
/// </summary>
public class DomainRules
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("  Ann@Acme.Example  ", "ann@acme.example")]
    [InlineData("ANN@ACME.EXAMPLE", "ann@acme.example")]
    [InlineData(null, "")]
    public void Email_addresses_normalise_before_comparison(string? input, string expected) =>
        Assert.Equal(expected, EmailAddress.Normalise(input));

    [Theory]
    [InlineData("ann@acme.example", true)]
    [InlineData("ann@acme", false)]
    [InlineData("ann.acme.example", false)]
    [InlineData("@acme.example", false)]
    [InlineData("", false)]
    public void Email_addresses_are_checked_syntactically(string input, bool expected) =>
        Assert.Equal(expected, EmailAddress.IsValid(input));

    [Fact]
    public void An_email_address_longer_than_254_characters_is_refused() =>
        Assert.False(EmailAddress.IsValid(new string('a', 250) + "@acme.example"));

    /// <summary>Section 5.3: minimum 12 characters, no composition rules, no maximum below 128.</summary>
    [Theory]
    [InlineData("12345678901", false)]
    [InlineData("123456789012", true)]
    [InlineData("            ", true)]   // twelve spaces: no composition rules means none
    public void Passwords_are_judged_only_on_length(string password, bool acceptable) =>
        Assert.Equal(acceptable, PasswordPolicy.Validate(password) is null);

    [Fact]
    public void A_password_of_128_characters_is_acceptable() =>
        Assert.Null(PasswordPolicy.Validate(new string('x', 128)));

    /// <summary>FR-008: five consecutive failures, then a fifteen-minute lockout.</summary>
    [Fact]
    public void Five_failures_lock_the_account()
    {
        var person = NewPerson();

        for (var i = 0; i < Person.MaxFailedAttempts - 1; i++)
        {
            person.RecordFailedSignIn(Now);
            Assert.False(person.IsLockedOut(Now));
        }

        person.RecordFailedSignIn(Now);

        Assert.True(person.IsLockedOut(Now));
        Assert.False(person.IsLockedOut(Now + Person.LockoutDuration));
    }

    /// <summary>EC-7: the window is not extended by attempts made during it.</summary>
    [Fact]
    public void A_failure_during_a_lockout_does_not_extend_it()
    {
        var person = NewPerson();
        for (var i = 0; i < Person.MaxFailedAttempts; i++)
        {
            person.RecordFailedSignIn(Now);
        }

        var lockedUntil = person.LockedUntil;
        person.RecordFailedSignIn(Now + TimeSpan.FromMinutes(5));

        Assert.Equal(lockedUntil, person.LockedUntil);
    }

    [Fact]
    public void A_successful_sign_in_clears_the_counter_and_the_lockout()
    {
        var person = NewPerson();
        person.RecordFailedSignIn(Now);
        person.RecordFailedSignIn(Now);

        person.RecordSuccessfulSignIn();

        Assert.Equal(0, person.FailedAttemptCount);
        Assert.Null(person.LockedUntil);
    }

    /// <summary>FR-014: an account always starts with the forced change armed.</summary>
    [Fact]
    public void A_new_account_must_change_its_password()
    {
        Assert.True(NewPerson().MustChangePassword);
    }

    [Fact]
    public void Setting_your_own_password_disarms_the_forced_change_and_an_admin_reset_re_arms_it()
    {
        var person = NewPerson();

        person.SetPassword("hash-1");
        Assert.False(person.MustChangePassword);

        person.SetAdminAssignedPassword("hash-2");
        Assert.True(person.MustChangePassword);
    }

    [Fact]
    public void Deactivation_stamps_the_time_and_reactivation_clears_it()
    {
        var person = NewPerson();

        person.Deactivate(Now);
        Assert.False(person.IsActive);
        Assert.Equal(Now, person.DeactivatedAt);

        person.Reactivate();
        Assert.True(person.IsActive);
        Assert.Null(person.DeactivatedAt);
    }

    /// <summary>NFR-001 and "Passwords are irreversible": the stored form reveals nothing.</summary>
    [Fact]
    public void A_stored_hash_contains_neither_the_password_nor_a_way_back_to_it()
    {
        var hasher = NewHasher();
        const string password = "correct horse battery";

        var hash = hasher.Hash(password);

        Assert.DoesNotContain(password, hash);
        Assert.StartsWith("pbkdf2-sha256$", hash);
        Assert.True(hasher.Verify(password, hash));
        Assert.False(hasher.Verify(password + "!", hash));
    }

    /// <summary>The per-person salt: the same password never produces the same stored value.</summary>
    [Fact]
    public void Two_people_with_the_same_password_get_different_hashes()
    {
        var hasher = NewHasher();

        Assert.NotEqual(hasher.Hash("correct horse battery"), hasher.Hash("correct horse battery"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2-sha256$notanumber$c2FsdA==$aGFzaA==")]
    [InlineData("argon2id$1$c2FsdA==$aGFzaA==")]
    public void An_unreadable_stored_hash_verifies_as_false_rather_than_throwing(string stored) =>
        Assert.False(NewHasher().Verify("anything at all", stored));

    private static Person NewPerson() =>
        Person.Create("Ann Devlin", "ann@acme.example", "hash-0", Role.User, Now);

    private static IPasswordHasher NewHasher() =>
        new Pbkdf2PasswordHasher(Options.Create(new PasswordHashingOptions { Iterations = 1000 }));
}
