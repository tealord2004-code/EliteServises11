using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICityValidationService _cityValidation;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICityValidationService cityValidation)
        {
            _context = context;
            _userManager = userManager;
            _cityValidation = cityValidation;
        }

        [BindProperty]
        public ServiceInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Автозаполнение из профиля
            Input.ProviderName = user.Company ?? $"{user.FirstName} {user.LastName}";
            Input.City = user.City;
            Input.Contacts = user.PhoneNumber2 ?? user.Email;

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrEmpty(Input.City))
            {
                var isValid = await _cityValidation.IsValidCityAsync(Input.City);
                if (!isValid)
                    ModelState.AddModelError("Input.City", "City must be from the list.");
            }

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var service = new Service
            {
                Title = System.Net.WebUtility.HtmlEncode(Input.Title),
                Category = Input.Category,
                ProviderType = Input.ProviderType,
                ProviderName = System.Net.WebUtility.HtmlEncode(Input.ProviderName),
                City = Input.City,
                Price = Input.Price,
                Contacts = Input.Contacts,
                Description = System.Net.WebUtility.HtmlEncode(Input.Description ?? ""),
                ProviderId = user.Id,
                PostedDate = DateTime.UtcNow,
                IsActive = true
            };
            // Проверка лимита подписки
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
            var tier = sub?.Tier ?? SubscriptionTier.Free;
            var isActive = sub?.IsActive ?? true;

            if (!isActive && tier != SubscriptionTier.Free)
            {
                tier = SubscriptionTier.Free;
            }

            var activeServices = await _context.Services.CountAsync(s => s.ProviderId == user.Id && s.IsActive);
            if (activeServices >= SubscriptionLimits.MaxServices(tier))
            {
                TempData["ErrorMessage"] = "Service limit reached. Upgrade your plan.";
                return RedirectToPage("/Profile/Upgrade", new { limit = "services" });
            }

            // Увеличиваем счётчик
            if (sub != null)
            {
                sub.ServicesUsed = activeServices + 1;
                await _context.SaveChangesAsync();
            }
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Service posted!";
            return RedirectToPage("./Index");
        }
    }

    public class ServiceInput
    {
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string ProviderType { get; set; } = "Физ. лицо";
        public string ProviderName { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Price { get; set; }
        public string? Contacts { get; set; }
        public string? Description { get; set; }
    }
}