using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Domain;
using pmsa.Security;

namespace pmsa.Pages.Account;

/// <summary>
/// FR-019 (a routine change) and FR-014 (the forced change at first sign-in) are the same screen:
/// both require the current password and both end with a password only the person knows.
/// </summary>
public class ChangePasswordModel(PasswordChangeService passwordChange) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>True when this is the blocking first-sign-in change rather than a voluntary one.</summary>
    public bool IsForced { get; private set; }

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Your current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A new password is required.")]
        [StringLength(PasswordPolicy.MaximumLength, MinimumLength = PasswordPolicy.MinimumLength,
            ErrorMessage = PasswordPolicy.LengthMessage)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The two new passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        IsForced = User.MustChangePassword();
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        IsForced = User.MustChangePassword();
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (User.GetPersonId() is not { } personId)
        {
            return RedirectToPage(AuthenticationDefaults.SignInPath);
        }

        var result = await passwordChange.ChangeAsync(
            personId, Input.CurrentPassword, Input.NewPassword, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            // SC-020: the password is unchanged and the reason is stated. Nothing is persisted on
            // any of these paths.
            ModelState.AddModelError(string.Empty, result.Message ?? "The password could not be changed.");
            return Page();
        }

        // NFR-004: re-issue the cookie so the identifier that existed before the password changed
        // is no longer usable. The fresh principal also drops the must-change-password claim,
        // which is what lets the person past ForcePasswordChangeFilter (FR-014).
        await HttpContext.SignOutAsync(AuthenticationDefaults.Scheme);
        await HttpContext.SignInAsync(
            AuthenticationDefaults.Scheme,
            PersonClaims.CreatePrincipal(result.Person!, AuthenticationDefaults.Scheme),
            new AuthenticationProperties { IsPersistent = false });

        // NFR-008: a forced change interrupts a journey, so it resumes it.
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        TempData["StatusMessage"] = "Your password has been changed.";
        return RedirectToPage("/Index");
    }
}
