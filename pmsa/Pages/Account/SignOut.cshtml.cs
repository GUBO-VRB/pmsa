using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Security;

namespace pmsa.Pages.Account;

/// <summary>
/// FR-006. Signing out is a POST so that it cannot be triggered by a link someone else planted,
/// and so that it is not something a prefetching browser can do on the person's behalf.
/// </summary>
public class SignOutModel : PageModel
{
    /// <summary>
    /// A GET here means the session already ended — usually the Back button after signing out.
    /// NFR-005 keeps the previous page out of the cache; this keeps the address harmless.
    /// </summary>
    public IActionResult OnGet() => RedirectToPage(AuthenticationDefaults.SignInPath);

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(AuthenticationDefaults.Scheme);
        return RedirectToPage(AuthenticationDefaults.SignInPath);
    }
}
