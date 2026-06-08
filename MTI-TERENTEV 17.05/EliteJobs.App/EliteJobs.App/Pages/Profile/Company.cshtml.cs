using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Profile
{
    public class CompanyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Company? Profile { get; set; }
        public List<Service> ActiveServices { get; set; } = new();
        public bool IsOwner { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            Company? company;

            if (id.HasValue)
            {
                company = await _context.Companies
                    .Include(c => c.Employer)
                    .FirstOrDefaultAsync(c => c.Id == id.Value);
            }
            else
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();
                company = await _context.Companies
                    .Include(c => c.Employer)
                    .FirstOrDefaultAsync(c => c.EmployerId == user.Id);
            }

            if (company == null) return Page();
            Profile = company;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                IsOwner = user?.Id == company.EmployerId;
            }

            ActiveServices = await _context.Services
                .Where(s => s.ProviderId == company.EmployerId && s.IsActive)
                .OrderByDescending(s => s.PostedDate)
                .ToListAsync();

            return Page();
        }
    }
}