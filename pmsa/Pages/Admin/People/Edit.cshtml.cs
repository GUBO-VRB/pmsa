using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Pages.Admin.People;

/// <summary>
/// FR-017 (change someone's role) and an Admin-set password reset, which re-arms FR-014.
/// </summary>
/// <remarks>
/// Neither action can be used to work around FR-018: both go through
/// <see cref="PeopleAdministrationService"/>, where the last-Admin invariant is checked under a
/// lock and a transaction.
/// </remarks>
public class EditModel(PeopleAdministrationService people) : PageModel
{
    public Person Person { get; private set; } = default!;

    [BindProperty]
    public Role Role { get; set; }

    [BindProperty]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    [StringLength(PasswordPolicy.MaximumLength, MinimumLength = PasswordPolicy.MinimumLength,
        ErrorMessage = PasswordPolicy.LengthMessage)]
    public string? NewPassword { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (await people.FindAsync(id, HttpContext.RequestAborted) is not { } person)
        {
            return NotFound();
        }

        Person = person;
        Role = person.Role;
        return Page();
    }

    public async Task<IActionResult> OnPostRoleAsync(Guid id)
    {
        var result = await people.ChangeRoleAsync(id, Role, HttpContext.RequestAborted);
        if (result.Succeeded)
        {
            // FR-017: no re-issued cookie and no forced sign-out. The new role is read from the
            // Person on their next request, which is exactly what SC-018 asks for.
            StatusMessage = $"{result.Person!.FullName} is now a {result.Person.Role}.";
            return RedirectToPage("Index");
        }

        return await RedisplayAsync(id, result.Message);
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(id, null);
        }

        var result = await people.ResetPasswordAsync(id, NewPassword ?? string.Empty, HttpContext.RequestAborted);
        if (result.Succeeded)
        {
            StatusMessage =
                $"{result.Person!.FullName}'s password has been set. " +
                "They will be asked to choose their own the next time they sign in.";
            return RedirectToPage("Index");
        }

        return await RedisplayAsync(id, result.Message);
    }

    private async Task<IActionResult> RedisplayAsync(Guid id, string? error)
    {
        if (await people.FindAsync(id, HttpContext.RequestAborted) is not { } person)
        {
            return NotFound();
        }

        Person = person;
        ErrorMessage = error;
        NewPassword = null;
        return Page();
    }
}
