using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EliteJobs.App.Pages.Profile
{
    [Authorize]
    public class WorkerEditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICityValidationService _cityValidation;

        public WorkerEditModel(UserManager<ApplicationUser> userManager, ICityValidationService cityValidation)
        {
            _userManager = userManager;
            _cityValidation = cityValidation;
        }

        [BindProperty]
        public ProfileInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            Input = new ProfileInput
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                PhoneNumber = user.PhoneNumber2,
                City = user.City,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                MaritalStatus = user.MaritalStatus,
                Citizenship = user.Citizenship,
                About = user.Position
            };

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

            user.FirstName = System.Net.WebUtility.HtmlEncode(Input.FirstName);
            user.LastName = System.Net.WebUtility.HtmlEncode(Input.LastName);
            user.MiddleName = Input.MiddleName;
            user.PhoneNumber2 = Input.PhoneNumber;
            user.City = Input.City;
            user.BirthDate = Input.BirthDate.HasValue
    ? DateTime.SpecifyKind(Input.BirthDate.Value, DateTimeKind.Utc)
    : null;
            user.Gender = Input.Gender;
            user.MaritalStatus = Input.MaritalStatus;
            user.Citizenship = Input.Citizenship;
            user.Position = Input.About;

            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Profile updated!";
            return RedirectToPage("/Profile/Worker");
        }
    }

    public class ProfileInput
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Citizenship { get; set; }
        public string? About { get; set; }
    }
}