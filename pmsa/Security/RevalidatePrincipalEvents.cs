using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using pmsa.Data;
using pmsa.Domain;

namespace pmsa.Security;

/// <summary>
/// Rebuilds the principal from the database on every request.
/// </summary>
/// <remarks>
/// The cookie carries only the person's id. Role, name and the forced-password-change flag are
/// read from the <see cref="Person"/> each time, which is what makes a role change (FR-017) and a
/// deactivation (FR-021) take effect at the very next request rather than at the next sign-in.
/// <para>
/// <see cref="CookieValidatePrincipalContext.ShouldRenew"/> is deliberately left false: replacing
/// the principal must not re-issue the cookie, because that would slide the 12-hour absolute
/// expiry that FR-007 requires activity <em>not</em> to extend.
/// </para>
/// </remarks>
public class RevalidatePrincipalEvents : CookieAuthenticationEvents
{
    /// <summary>Where the current request's <see cref="Person"/> is cached, so pages need not re-query.</summary>
    public const string CurrentPersonItemKey = "pmsa:current-person";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var personId = context.Principal?.GetPersonId();
        if (personId is null)
        {
            await RejectAsync(context);
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<PmsaDbContext>();
        var person = await db.People
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == personId.Value, context.HttpContext.RequestAborted);

        // "No session outlives its account": the check happens per request, not per sign-in.
        if (person is null || !person.IsActive)
        {
            await RejectAsync(context);
            return;
        }

        context.HttpContext.Items[CurrentPersonItemKey] = person;
        context.ReplacePrincipal(PersonClaims.CreatePrincipal(person, context.Scheme.Name));
        context.ShouldRenew = false;
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
