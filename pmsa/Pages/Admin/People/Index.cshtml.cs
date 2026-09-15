using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Pages.Admin.People;

/// <summary>
/// The people list and the deactivate/reactivate actions (FR-015, FR-016).
/// </summary>
/// <remarks>
/// NFR-009 sizes this for up to 200 people, which is why it is one unpaginated list with no
/// search — below that number both would cost more than they give.
/// </remarks>
public class IndexModel(PeopleAdministrationService people) : PageModel
{
    public IReadOnlyList<Person> People { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync() => People = await people.ListAsync(HttpContext.RequestAborted);

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        var result = await people.DeactivateAsync(id, HttpContext.RequestAborted);
        return Apply(result, "{0} can no longer sign in. Their logged hours are untouched.");
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid id)
    {
        var result = await people.ReactivateAsync(id, HttpContext.RequestAborted);
        return Apply(result, "{0} can sign in again with their previous password.");
    }

    private IActionResult Apply(AdministrationResult result, string successTemplate)
    {
        if (result.Succeeded)
        {
            StatusMessage = string.Format(successTemplate, result.Person!.FullName);
        }
        else
        {
            // SC-019: the last-Admin refusal says why, rather than failing silently.
            ErrorMessage = result.Message ?? "That change could not be made.";
        }

        return RedirectToPage();
    }
}
