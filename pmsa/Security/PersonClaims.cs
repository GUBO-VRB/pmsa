using System.Security.Claims;
using pmsa.Domain;

namespace pmsa.Security;

/// <summary>
/// Translates a <see cref="Person"/> into the principal the rest of the request sees.
/// </summary>
/// <remarks>
/// The role claim is rebuilt from the database on <em>every</em> request (see
/// <c>RevalidatePrincipal</c>), which is what makes FR-017 and FR-021 immediate. Only the
/// person's id is genuinely carried by the cookie; everything else is a per-request projection.
/// </remarks>
public static class PersonClaims
{
    public const string MustChangePasswordClaim = "pmsa:must_change_password";

    public static ClaimsPrincipal CreatePrincipal(Person person, string authenticationScheme)
    {
        var identity = new ClaimsIdentity(
            authenticationType: authenticationScheme,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, person.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, person.FullName));
        identity.AddClaim(new Claim(ClaimTypes.Email, person.Email));
        identity.AddClaim(new Claim(ClaimTypes.Role, person.Role.ToString()));

        if (person.MustChangePassword)
        {
            identity.AddClaim(new Claim(MustChangePasswordClaim, "true"));
        }

        return new ClaimsPrincipal(identity);
    }

    public static Guid? GetPersonId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static Role? GetRole(this ClaimsPrincipal principal) =>
        Enum.TryParse<Role>(principal.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

    public static string GetFullName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public static bool MustChangePassword(this ClaimsPrincipal principal) =>
        principal.HasClaim(MustChangePasswordClaim, "true");

    /// <summary>Manager and Admin share every capability except account administration (Section 2.1).</summary>
    public static bool IsManagerOrAbove(this ClaimsPrincipal principal) =>
        principal.GetRole() is Role.Manager or Role.Admin;

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.GetRole() is Role.Admin;
}
