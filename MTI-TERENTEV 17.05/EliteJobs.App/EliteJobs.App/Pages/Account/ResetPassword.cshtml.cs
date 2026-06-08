using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace EliteJobs.App.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public ResetPasswordInput Input { get; set; } = new();
        public bool PasswordReset { get; set; }

        public IActionResult OnGet(string token, string email)
        {
            Input.Token = token;
            Input.Email = email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                PasswordReset = true;
                return Page();
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);

            if (result.Succeeded)
                PasswordReset = true;
            else
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }

    public class ResetPasswordInput
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}