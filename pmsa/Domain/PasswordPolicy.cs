namespace pmsa.Domain;

/// <summary>
/// Spec 007, Section 5.3: minimum 12 characters, no composition rules, no maximum below 128.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 128;

    public const string LengthMessage =
        "Password must be at least 12 characters long.";

    /// <summary>Returns null when the password is acceptable, otherwise the reason for refusal.</summary>
    public static string? Validate(string? password) => password switch
    {
        null or "" => "Password is required.",
        { Length: < MinimumLength } => LengthMessage,
        { Length: > MaximumLength } => $"Password must be at most {MaximumLength} characters long.",
        _ => null,
    };
}
