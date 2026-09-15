namespace pmsa.Security;

public static class AuthenticationDefaults
{
    public const string Scheme = "pmsa";

    public const string CookieName = "pmsa.session";

    /// <summary>
    /// FR-007: an absolute 12-hour expiry, never extended by activity. Combined with a
    /// non-persistent cookie, the session also dies when the browser closes.
    /// </summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);

    public const string SignInPath = "/Account/SignIn";
    public const string SignOutPath = "/Account/SignOut";
    public const string ChangePasswordPath = "/Account/ChangePassword";
    public const string ForbiddenPath = "/Account/Forbidden";
}

public static class AuthorizationPolicies
{
    /// <summary>Projects, budgets, assignments and other people's entries (FR-011).</summary>
    public const string RequireManager = "RequireManager";

    /// <summary>Account creation, role changes and deactivation (Section 2.1, last row).</summary>
    public const string RequireAdmin = "RequireAdmin";
}
