using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Profile
{
    [Authorize]
    public class CompanyEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICityValidationService _cityValidation;

        public CompanyEditModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICityValidationService cityValidation)
        {
            _context = context;
            _userManager = userManager;
            _cityValidation = cityValidation;
        }

        [BindProperty]
        public CompanyInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.EmployerId == user.Id);

            if (company != null)
            {
                Input = new CompanyInput
                {
                    Name = company.Name,
                    Description = company.Description,
                    Website = company.Website,
                    City = company.City,
                    Address = company.Address,
                    NearestMetro = company.NearestMetro,
                    PublicTransport = company.PublicTransport,
                    Industry = company.Industry,
                    EmployeesCount = company.EmployeesCount,
                    EmploymentType = company.EmploymentType,
                    HasParking = company.HasParking
                };
            }
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            // Валидация города
            if (!string.IsNullOrEmpty(Input.City))
            {
                var isValid = await _cityValidation.IsValidCityAsync(Input.City);
                if (!isValid)
                {
                    ModelState.AddModelError("Input.City", "Город должен быть выбран из списка.");
                }
            }

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.EmployerId == user.Id);

            if (company == null)
            {
                company = new Company { EmployerId = user.Id, CreatedDate = DateTime.UtcNow };
                _context.Companies.Add(company);
            }

            company.Name = System.Net.WebUtility.HtmlEncode(Input.Name);
            company.Description = Input.Description != null ? System.Net.WebUtility.HtmlEncode(Input.Description) : null;
            company.Website = Input.Website;
            company.City = Input.City;
            company.Address = Input.Address;
            company.NearestMetro = Input.NearestMetro;
            company.PublicTransport = Input.PublicTransport;
            company.Industry = Input.Industry;
            company.EmployeesCount = Input.EmployeesCount;
            company.EmploymentType = Input.EmploymentType;
            company.HasParking = Input.HasParking;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Company profile saved!";
            return RedirectToPage("/Profile/Company");
        }
    }

    public class CompanyInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? NearestMetro { get; set; }
        public string? PublicTransport { get; set; }
        public string? Industry { get; set; }
        public string? EmployeesCount { get; set; }
        public string? EmploymentType { get; set; }
        public bool HasParking { get; set; }
    }
}