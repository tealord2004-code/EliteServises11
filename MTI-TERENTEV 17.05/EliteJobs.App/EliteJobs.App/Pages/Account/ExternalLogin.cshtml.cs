using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EliteJobs.App.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string CLIENT_ID = "922686645597-8m73hvoj2mef7e3ik8v3aqiqm89apb9d.apps.googleusercontent.com";
        private const string CLIENT_SECRET = "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";

        public string RedirectUrl { get; set; } = "";

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult OnGet()
        {
            var redirectUri = $"{Request.Scheme}://{Request.Host}/Account/ExternalLogin?handler=Callback";

            RedirectUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={CLIENT_ID}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                "&response_type=code" +
                "&scope=email%20profile" +
                "&access_type=offline";

            return Page();
        }

        public async Task<IActionResult> OnGetCallbackAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Authorization cancelled.";
                return RedirectToPage("/Account/Login");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var redirectUri = $"{Request.Scheme}://{Request.Host}/Account/ExternalLogin?handler=Callback";

                var tokenResponse = await client.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["code"] = code,
                        ["client_id"] = CLIENT_ID,
                        ["client_secret"] = CLIENT_SECRET,
                        ["redirect_uri"] = redirectUri,
                        ["grant_type"] = "authorization_code"
                    }));

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

                if (tokenData.TryGetProperty("error", out var err))
                {
                    TempData["ErrorMessage"] = $"Google error: {err.GetString()}";
                    return RedirectToPage("/Account/Login");
                }

                var accessToken = tokenData.GetProperty("access_token").GetString();

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var userResponse = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<JsonElement>(userJson);

                var email = userData.GetProperty("email").GetString();
                var googleId = userData.GetProperty("id").GetString();

                var user = await _userManager.FindByLoginAsync("Google", googleId!);

                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(email!);

                    if (user != null)
                    {
                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId!, "Google"));
                    }
                    else
                    {
                        var firstName = userData.TryGetProperty("given_name", out var gn) ? gn.GetString() : "";
                        var lastName = userData.TryGetProperty("family_name", out var ln) ? ln.GetString() : "";

                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FirstName = firstName ?? "",
                            LastName = lastName ?? "",
                            RegisteredDate = DateTime.UtcNow,
                            IsActive = true,
                            EmailConfirmed = true
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            TempData["ErrorMessage"] = "Failed to create account.";
                            return RedirectToPage("/Account/Login");
                        }

                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId!, "Google"));
                        await _userManager.AddToRoleAsync(user, "CustomerIndividual");
                        _context.Subscriptions.Add(new Subscription { UserId = user.Id, Tier = SubscriptionTier.Free });
                        await _context.SaveChangesAsync();
                    }
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = "Signed in with Google!";

                if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
                    return RedirectToPage("/Profile/WorkerEdit");

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("ProviderIndividual"))
                {
                    var hasResume = await _context.Resumes.AnyAsync(r => r.WorkerId == user.Id && r.IsActive);
                    if (!hasResume) return RedirectToPage("/Profile/ResumeEdit");
                }
                if (roles.Contains("ProviderCompany"))
                {
                    var hasCompany = await _context.Companies.AnyAsync(c => c.EmployerId == user.Id);
                    if (!hasCompany) return RedirectToPage("/Profile/CompanyEdit");
                }

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToPage("/Account/Login");
            }
        }
    }
}