using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EliteJobs.App.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly RecaptchaService _recaptchaService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            RecaptchaService recaptchaService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _recaptchaService = recaptchaService;
            RecaptchaSiteKey = Environment.GetEnvironmentVariable("RECAPTCHA_SITE_KEY")
                ?? configuration["Recaptcha:SiteKey"]
                ?? "";
        }

        public string RecaptchaSiteKey { get; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
    string firstName, string lastName, string? middleName,
    string email, string password, string confirmPassword,
    string role, string? company, string? recaptchaToken)
        {
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("REGISTER POST RECEIVED!");
                Console.WriteLine($"email: {email}");
                Console.WriteLine($"role: {role}");
                Console.WriteLine($"recaptchaToken length: {recaptchaToken?.Length ?? 0}");
                Console.WriteLine("========================================");

                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                { ErrorMessage = Messages.FillName; return Page(); }
                if (string.IsNullOrEmpty(email))
                { ErrorMessage = Messages.FillEmail; return Page(); }
                if (string.IsNullOrEmpty(password) || password.Length < 8)
                { ErrorMessage = Messages.PasswordShort; return Page(); }
                if (password != confirmPassword)
                { ErrorMessage = Messages.PasswordsMismatch; return Page(); }
                if (string.IsNullOrEmpty(role))
                { ErrorMessage = Messages.SelectRole; return Page(); }

                var captchaOk = await _recaptchaService.VerifyAsync(recaptchaToken ?? "");
                Console.WriteLine($"captchaOk: {captchaOk}");

                if (!captchaOk)
                { ErrorMessage = Messages.CaptchaFailed; return Page(); }

                Console.WriteLine("Creating user...");

                string identityRole = role switch
                {
                    "customer-individual" => "CustomerIndividual",
                    "provider-individual" => "ProviderIndividual",
                    "customer-company" => "CustomerCompany",
                    "provider-company" => "ProviderCompany",
                    _ => "CustomerIndividual"
                };

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    MiddleName = middleName,
                    Company = role.Contains("company") ? company : null,
                    RegisteredDate = DateTime.UtcNow,
                    IsActive = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                Console.WriteLine($"Calling CreateAsync for {email}...");
                var result = await _userManager.CreateAsync(user, password);
                Console.WriteLine($"CreateAsync result: Succeeded={result.Succeeded}");

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => $"[{e.Code}] {e.Description}").ToList();
                    Console.WriteLine($"Errors: {string.Join(" | ", errors)}");
                    ErrorMessage = string.Join("; ", errors);
                    return Page();
                }

                Console.WriteLine("User created! Adding role...");
                await _userManager.AddToRoleAsync(user, identityRole);

                Console.WriteLine("Adding subscription...");
                _context.Subscriptions.Add(new Subscription { UserId = user.Id, Tier = SubscriptionTier.Free });
                await _context.SaveChangesAsync();

                Console.WriteLine("Signing in...");
                await _signInManager.SignInAsync(user, isPersistent: false);
                Console.WriteLine("DONE! Redirecting...");

                if (identityRole == "ProviderIndividual") return RedirectToPage("/Profile/ResumeEdit");
                if (identityRole == "ProviderCompany") return RedirectToPage("/Profile/CompanyEdit");
                return RedirectToPage("/Profile/Worker");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"STACK: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    Console.WriteLine($"INNER STACK: {ex.InnerException.StackTrace}");
                }
                ErrorMessage = $"Error: {ex.Message}";
                return Page();
            }
        }
    }
}