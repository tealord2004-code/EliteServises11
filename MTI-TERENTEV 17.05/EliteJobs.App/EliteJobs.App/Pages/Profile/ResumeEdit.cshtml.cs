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
    public class ResumeEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICityValidationService _cityValidation;

        public ResumeEditModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICityValidationService cityValidation)
        {
            _context = context;
            _userManager = userManager;
            _cityValidation = cityValidation;
        }

        [BindProperty]
        public ResumeInput Input { get; set; } = new();
        public bool IsNew { get; set; } = true;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                IsNew = false;
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var resume = await _context.Resumes
                    .FirstOrDefaultAsync(r => r.Id == id && r.WorkerId == user.Id);
                if (resume == null) return NotFound();

                Input = new ResumeInput
                {
                    DesiredPosition = resume.DesiredPosition,
                    DesiredSalary = resume.DesiredSalary,
                    City = resume.City,
                    EmploymentType = resume.EmploymentType,
                    WorkSchedule = resume.WorkSchedule,
                    Experience = resume.Experience,
                    Education = resume.Education,
                    Skills = resume.Skills,
                    Languages = resume.Languages,
                    About = resume.About,
                    ReadyForRemote = resume.ReadyForRemote,
                    ReadyToRelocate = resume.ReadyToRelocate,
                    HasCar = resume.HasCar,
                    DrivingLicense = resume.DrivingLicense,
                    IsActive = resume.IsActive
                };
            }
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int? id)
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

            Resume resume;
            if (id.HasValue)
            {
                IsNew = false;
                resume = await _context.Resumes
                    .FirstOrDefaultAsync(r => r.Id == id && r.WorkerId == user.Id);
                if (resume == null) return NotFound();
            }
            else
            {
                IsNew = true;
                resume = new Resume { WorkerId = user.Id, CreatedDate = DateTime.UtcNow };
                _context.Resumes.Add(resume);
            }

            resume.DesiredPosition = System.Net.WebUtility.HtmlEncode(Input.DesiredPosition);
            resume.DesiredSalary = Input.DesiredSalary;
            resume.City = Input.City;
            resume.EmploymentType = Input.EmploymentType;
            resume.WorkSchedule = Input.WorkSchedule;
            resume.Experience = Input.Experience != null ? System.Net.WebUtility.HtmlEncode(Input.Experience) : null;
            resume.Education = Input.Education != null ? System.Net.WebUtility.HtmlEncode(Input.Education) : null;
            resume.Skills = Input.Skills;
            resume.Languages = Input.Languages;
            resume.About = Input.About != null ? System.Net.WebUtility.HtmlEncode(Input.About) : null;
            resume.ReadyForRemote = Input.ReadyForRemote;
            resume.ReadyToRelocate = Input.ReadyToRelocate;
            resume.HasCar = Input.HasCar;
            resume.DrivingLicense = Input.DrivingLicense;
            resume.IsActive = Input.IsActive;
            resume.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = IsNew ? "Resume created!" : "Resume updated!";
            return RedirectToPage("/Profile/Worker");
        }
    }

    public class ResumeInput
    {
        public string DesiredPosition { get; set; } = string.Empty;
        public string? DesiredSalary { get; set; }
        public string? City { get; set; }
        public string? EmploymentType { get; set; }
        public string? WorkSchedule { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? Skills { get; set; }
        public string? Languages { get; set; }
        public string? About { get; set; }
        public bool ReadyForRemote { get; set; } = true;
        public bool ReadyToRelocate { get; set; } = false;
        public bool HasCar { get; set; }
        public string? DrivingLicense { get; set; }
        public bool IsActive { get; set; } = true;
    }
}