using EliteJobs.App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICityValidationService _cityValidation;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICityValidationService cityValidation)
        {
            _context = context;
            _userManager = userManager;
            _cityValidation = cityValidation;
        }

        [BindProperty]
        public ServiceEditInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var service = await _context.Services.FindAsync(Id);
            if (service == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (service.ProviderId != user?.Id && !User.IsInRole("Admin"))
                return Forbid();

            Input = new ServiceEditInput
            {
                Title = service.Title,
                Category = service.Category,
                ProviderType = service.ProviderType ?? "Физ. лицо",
                ProviderName = service.ProviderName ?? "",
                City = service.City,
                Price = service.Price,
                Contacts = service.Contacts,
                Description = service.Description,
                IsActive = service.IsActive
            };

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var service = await _context.Services.FindAsync(Id);
            if (service == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (service.ProviderId != user?.Id && !User.IsInRole("Admin"))
                return Forbid();

            if (!string.IsNullOrEmpty(Input.City))
            {
                var isValid = await _cityValidation.IsValidCityAsync(Input.City);
                if (!isValid) ModelState.AddModelError("Input.City", "Город должен быть из списка.");
            }

            if (!ModelState.IsValid) return Page();

            service.Title = System.Net.WebUtility.HtmlEncode(Input.Title);
            service.Category = Input.Category;
            service.ProviderType = Input.ProviderType;
            service.ProviderName = System.Net.WebUtility.HtmlEncode(Input.ProviderName);
            service.City = Input.City;
            service.Price = Input.Price;
            service.Contacts = Input.Contacts;
            service.Description = System.Net.WebUtility.HtmlEncode(Input.Description ?? "");
            service.IsActive = Input.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Услуга обновлена!";
            return RedirectToPage("/Details", new { id = Id });
        }
    }

    public class ServiceEditInput
    {
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string ProviderType { get; set; } = "Физ. лицо";
        public string ProviderName { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Price { get; set; }
        public string? Contacts { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}