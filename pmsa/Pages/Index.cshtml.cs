using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Pages;

/// <summary>
/// Where a successful sign-in lands (SC-001). Today's entries belong to spec 001, so for now
/// this page does the part spec 007 owns: it identifies the signed-in person and shows what
/// their role reaches.
/// </summary>
public class IndexModel : PageModel
{
    public string FullName { get; private set; } = string.Empty;

    public Role Role { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        FullName = User.GetFullName();
        Role = User.GetRole() ?? Role.User;
    }
}
