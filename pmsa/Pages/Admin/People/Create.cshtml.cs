using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Pages.Admin.People;

/// <summary>
/// FR-013: an Admin creates a person with a full name, a unique email address, a role and an
/// initial password. SUC-004 is the bar — a working account in under two minutes with no
/// database access — so this is one short form and nothing else.
/// </summary>
public class CreateModel(PeopleAdministrationService people) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "A full name is required.")]
        [StringLength(Person.FullNameMaxLength, MinimumLength = 1,
            ErrorMessage = "A full name of 1 to 200 characters is required.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "An email address is required.")]
        [StringLength(EmailAddress.MaxLength,
            ErrorMessage = "An email address may be at most 254 characters long.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public Role Role { get; set; } = Role.User;

        [Required(ErrorMessage = "An initial password is required.")]
        [StringLength(PasswordPolicy.MaximumLength, MinimumLength = PasswordPolicy.MinimumLength,
            ErrorMessage = PasswordPolicy.LengthMessage)]
        [DataType(DataType.Password)]
        [Display(Name = "Initial password")]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // EC-11: an over-length name or address is refused with the limit named, by the
        // StringLength messages above.
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!EmailAddress.IsValid(Input.Email))
        {
            ModelState.AddModelError("Input.Email", "That does not look like an email address.");
            return Page();
        }

        var result = await people.CreateAsync(
            Input.FullName, Input.Email, Input.Role, Input.Password, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            // SC-015: a duplicate address is reported against the field that caused it.
            var key = result.Outcome is AdministrationOutcome.EmailInUse ? "Input.Email" : string.Empty;
            ModelState.AddModelError(key, result.Message ?? "The account could not be created.");
            return Page();
        }

        // FR-014: they will be asked to replace this password the first time they sign in, which
        // is the only reason it is safe for an Admin to have chosen it.
        StatusMessage =
            $"{result.Person!.FullName} can now sign in as {result.Person.Email}. " +
            "They will be asked to choose their own password.";

        return RedirectToPage("Index");
    }
}
