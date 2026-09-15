using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace pmsa.Security;

/// <summary>
/// FR-014 and the <em>Forced change is blocking</em> invariant: while a person's password was
/// last set by an Admin, they can reach nothing but the password-change screen and sign-out.
/// </summary>
/// <remarks>
/// This is a server-side refusal, not a navigation tweak — it applies to every page, including
/// ones addressed directly, which is what SC-016 ("before reaching any other page") asks for.
/// </remarks>
public class ForcePasswordChangeFilter : IAsyncPageFilter
{
    private static readonly string[] AllowedPaths =
    [
        AuthenticationDefaults.ChangePasswordPath,
        AuthenticationDefaults.SignOutPath,
        AuthenticationDefaults.SignInPath,
        AuthenticationDefaults.ForbiddenPath,
        "/Error",
    ];

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated is true && user.MustChangePassword() && !IsAllowed(context))
        {
            context.Result = new RedirectToPageResult(AuthenticationDefaults.ChangePasswordPath);
            return;
        }

        await next();
    }

    private static bool IsAllowed(PageHandlerExecutingContext context)
    {
        var page = (context.ActionDescriptor as Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor)?.ViewEnginePath
                   ?? context.HttpContext.Request.Path.Value
                   ?? string.Empty;

        return AllowedPaths.Any(allowed => string.Equals(page, allowed, StringComparison.OrdinalIgnoreCase));
    }
}
