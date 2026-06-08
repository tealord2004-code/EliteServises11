using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EliteJobs.App.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RecaptchaService _recaptchaService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RecaptchaService recaptchaService,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _recaptchaService = recaptchaService;
            RecaptchaSiteKey = Environment.GetEnvironmentVariable("RECAPTCHA_SITE_KEY")
                ?? configuration["Recaptcha:SiteKey"]
                ?? "";
        }

        public string? ErrorMessage { get; set; }
        public string EmailValue { get; set; } = "";
        public string RecaptchaSiteKey { get; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostLoginAsync(
            string email, string password, bool rememberMe,
            string? recaptchaToken = null, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            EmailValue = email;

            var captchaOk = await _recaptchaService.VerifyAsync(recaptchaToken ?? "");
            if (!captchaOk) { ErrorMessage = Messages.CaptchaFailed; return Page(); }
            if (string.IsNullOrEmpty(email)) { ErrorMessage = Messages.FillEmail; return Page(); }
            if (string.IsNullOrEmpty(password)) { ErrorMessage = "Введите пароль."; return Page(); }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) { ErrorMessage = "Неверный email или пароль."; return Page(); }
            if (!user.IsActive) { ErrorMessage = "Аккаунт заблокирован."; return Page(); }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: true);
            if (result.Succeeded) return LocalRedirect(returnUrl);
            if (result.IsLockedOut) { ErrorMessage = "Аккаунт заблокирован. Попробуйте позже."; return Page(); }

            ErrorMessage = "Неверный email или пароль.";
            return Page();
        }
    }
}