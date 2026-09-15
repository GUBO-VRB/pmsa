using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pmsa.Pages.Account;

/// <summary>
/// FR-010 and SC-011: what a request for a capability above your role gets. It states the
/// refusal and nothing else — naming the resource, or hinting at its contents, would disclose
/// exactly what the refusal exists to withhold.
/// </summary>
public class ForbiddenModel : PageModel
{
    public IActionResult OnGet()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Page();
    }
}
