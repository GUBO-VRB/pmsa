using System.Text.RegularExpressions;

namespace pmsa.Domain;

/// <summary>
/// The sign-in identifier (spec 007, Section 5.3). Normalisation is the whole point of this
/// type: " Ann@Acme.Example " and "ann@acme.example" are the same person (EC-2), so the
/// normalised form is what is compared and what is stored.
/// </summary>
public static partial class EmailAddress
{
    /// <summary>Maximum stored length, per the domain model.</summary>
    public const int MaxLength = 254;

    /// <summary>Trims surrounding whitespace and lower-cases, so comparison is case-insensitive.</summary>
    public static string Normalise(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// A deliberately permissive syntactic check: one "@" with something either side and a dot
    /// in the domain. Anything stricter rejects addresses that are valid in practice.
    /// </summary>
    public static bool IsValid(string? value)
    {
        var normalised = Normalise(value);
        return normalised.Length is > 0 and <= MaxLength && Syntax().IsMatch(normalised);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex Syntax();
}
