using EliteJobs.App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EliteJobs.App.Pages.Profile
{
    [Authorize]
    public class LinkGoogleModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly HttpClient _httpClient;

        public LinkGoogleModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpClient = httpClientFactory.CreateClient();
        }

        public bool IsLinked { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            var logins = await _userManager.GetLoginsAsync(user);
            IsLinked = logins.Any(l => l.LoginProvider == "Google");
        }

        public IActionResult OnPostLink()
        {
            var redirectUri = $"{Request.Scheme}://{Request.Host}/Profile/LinkGoogle?handler=Callback";
            var clientId = "922686645597-8m73hvoj2mef7e3ik8v3aqiqm89apb9d.apps.googleusercontent.com";

            var googleUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={clientId}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                "&response_type=code" +
                "&scope=email%20profile" +
                "&access_type=offline";

            return Redirect(googleUrl);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "No code received.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            try
            {
                var clientId = "922686645597-8m73hvoj2mef7e3ik8v3aqiqm89apb9d.apps.googleusercontent.com";
                var clientSecret = "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";
                var redirectUri = $"{Request.Scheme}://{Request.Host}/Profile/LinkGoogle?handler=Callback";

                var tokenResponse = await _httpClient.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["code"] = code,
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["redirect_uri"] = redirectUri,
                        ["grant_type"] = "authorization_code"
                    }));

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(tokenJson);

                if (tokenData.TryGetProperty("error", out var error))
                {
                    TempData["ErrorMessage"] = $"Google error: {error.GetString()}";
                    return RedirectToPage();
                }

                var accessToken = tokenData.GetProperty("access_token").GetString();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var userResponse = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(userJson);
                var googleId = userData.GetProperty("id").GetString();

                var existingLogins = await _userManager.GetLoginsAsync(user);
                if (existingLogins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == googleId))
                {
                    TempData["ErrorMessage"] = "Already linked.";
                    return RedirectToPage();
                }

                await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId!, "Google"));
                TempData["SuccessMessage"] = "Google linked!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}