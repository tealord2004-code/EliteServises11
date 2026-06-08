using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Profile
{
    [Authorize]
    public class AddRoleModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public AddRoleModel(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public List<int> CurrentRoleIds { get; set; } = new();
        public List<int> AvailableRoleIds { get; set; } = new();
        public string? Message { get; set; }
        public bool Success { get; set; }

        public static readonly Dictionary<int, string> RoleNames = new()
        {
            { 1, "CustomerIndividual" },
            { 2, "ProviderIndividual" },
            { 3, "CustomerCompany" },
            { 4, "ProviderCompany" }
        };

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            var currentNames = (await _userManager.GetRolesAsync(user)).Where(r => r != "Admin").ToList();
            CurrentRoleIds = currentNames
                .Select(r => RoleNames.FirstOrDefault(x => x.Value == r).Key)
                .Where(id => id > 0)
                .ToList();
            AvailableRoleIds = RoleNames
                .Where(r => !currentNames.Contains(r.Value))
                .Select(r => r.Key)
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(int newRole)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!RoleNames.ContainsKey(newRole))
            {
                Message = "Invalid role.";
                await LoadRoles();
                return Page();
            }

            var roleName = RoleNames[newRole];
            if ((await _userManager.GetRolesAsync(user)).Contains(roleName))
            {
                Message = "Already assigned.";
                await LoadRoles();
                return Page();
            }

            // Проверяем готовность профиля перед выдачей роли
            bool needProfile = newRole switch
            {
                2 => !await _dbContext.Resumes.AnyAsync(r => r.WorkerId == user.Id && r.IsActive),
                4 => !await _dbContext.Companies.AnyAsync(c => c.EmployerId == user.Id),
                _ => false
            };

            if (needProfile)
            {
                // Сначала выдаём роль, потом редиректим
                await _userManager.AddToRoleAsync(user, roleName);

                string redirectPage = newRole switch
                {
                    2 => "/Profile/ResumeEdit",
                    4 => "/Profile/CompanyEdit",
                    _ => "/Profile/WorkerEdit"
                };
                TempData["SuccessMessage"] = "Role added! Fill the profile.";
                return Redirect(redirectPage);
            }

            // Всё готово — просто выдаём роль
            await _userManager.AddToRoleAsync(user, roleName);
            Success = true;
            await LoadRoles();

            TempData["SuccessMessage"] = "Role added!";
            return RedirectToPage("/Profile/Worker");
        }

        private async Task LoadRoles()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var currentNames = (await _userManager.GetRolesAsync(user)).Where(r => r != "Admin").ToList();
                CurrentRoleIds = currentNames
                    .Select(r => RoleNames.FirstOrDefault(x => x.Value == r).Key)
                    .Where(id => id > 0)
                    .ToList();
                AvailableRoleIds = RoleNames
                    .Where(r => !currentNames.Contains(r.Value))
                    .Select(r => r.Key)
                    .ToList();
            }
        }
    }
}