using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using pmsa.Security;

namespace pmsa.Pages.Account;

[AllowAnonymous]
public class SignInModel(SignInService signIn) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Arriving at sign-in with a live session means the previous one is being abandoned;
        // clearing it keeps "signed in as" from disagreeing with what is about to happen.
        if (User.Identity?.IsAuthenticated is true)
        {
            await HttpContext.SignOutAsync(AuthenticationDefaults.Scheme);
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // EC-1: an empty field is a validation failure, not an authentication attempt — so it
        // neither reaches the hasher nor moves the lockout counter.
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signIn.AuthenticateAsync(Input.Email, Input.Password, HttpContext.RequestAborted);
        if (!result.Succeeded || result.Person is null)
        {
            ModelState.AddModelError(string.Empty, SignInAttemptResult.RefusalMessage);
            return Page();
        }

        // FR-007: a non-persistent cookie dies with the browser, and the handler stamps the
        // ticket with IssuedUtc + ExpireTimeSpan — a 12-hour ceiling activity does not extend.
        var properties = new AuthenticationProperties { IsPersistent = false };

        // NFR-004: SignInAsync writes a fresh cookie, so the pre-authentication session
        // identifier cannot be replayed (session-fixation resistance).
        await HttpContext.SignInAsync(
            AuthenticationDefaults.Scheme,
            PersonClaims.CreatePrincipal(result.Person, AuthenticationDefaults.Scheme),
            properties);

        if (result.Person.MustChangePassword)
        {
            // FR-014: the forced change comes before anything else, including the return URL.
            return RedirectToPage(AuthenticationDefaults.ChangePasswordPath, new { returnUrl });
        }

        // NFR-008: back to whatever was originally asked for. LocalRedirect refuses an absolute
        // URL, so a crafted returnUrl cannot bounce anyone off-site.
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Index");
    }
}
