using EliteJobs.App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EliteJobs.App.Pages.Profile
{
    [Authorize]
    public class Enable2FAModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Enable2FAModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public bool Is2FAEnabled { get; set; }
        public string? ManualKey { get; set; }
        public string? QrCodeUri { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            Is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(user!);

            if (!Is2FAEnabled)
            {
                var key = await _userManager.GetAuthenticatorKeyAsync(user!);
                if (string.IsNullOrEmpty(key))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(user!);
                    key = await _userManager.GetAuthenticatorKeyAsync(user!);
                }
                ManualKey = key;
                QrCodeUri = $"otpauth://totp/Elite:{user!.Email}?secret={key}&issuer=Elite";
            }
        }

        public async Task<IActionResult> OnPostEnableAsync(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (await _userManager.VerifyTwoFactorTokenAsync(user!, TokenOptions.DefaultAuthenticatorProvider, code))
            {
                await _userManager.SetTwoFactorEnabledAsync(user!, true);
                TempData["SuccessMessage"] = "2FA enabled!";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisableAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            await _userManager.SetTwoFactorEnabledAsync(user!, false);
            TempData["SuccessMessage"] = "2FA disabled!";
            return RedirectToPage();
        }
    }
}