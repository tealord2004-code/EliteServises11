using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EliteJobs.App.Pages.Account
{
    public class AdminLoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AdminLoginModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (User.IsInRole("Admin"))
                return RedirectToPage("/Admin/Dashboard");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string email, string password, string accessKey)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(accessKey))
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            // Проверяем ключ доступа
            var keyHash = HashKey(accessKey);
            var validKey = await _context.AdminAccessKeys
                .AnyAsync(k => k.KeyHash == keyHash && k.IsActive);

            if (!validKey)
            {
                ErrorMessage = "Invalid security key.";
                return Page();
            }

            // Проверяем пользователя
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ErrorMessage = "Invalid admin credentials.";
                return Page();
            }

            if (!user.IsActive)
            {
                ErrorMessage = "Admin account is deactivated.";
                return Page();
            }

            // Вход
            var result = await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            ErrorMessage = "Invalid credentials.";
            return Page();
        }

        private static string HashKey(string key)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}