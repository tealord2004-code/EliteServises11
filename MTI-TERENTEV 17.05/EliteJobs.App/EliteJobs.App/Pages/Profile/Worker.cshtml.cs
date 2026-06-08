using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Profile
{
    public class WorkerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorkerModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser? ProfileUser { get; set; }
        public Company? CompanyProfile { get; set; }
        public List<Service> MyServices { get; set; } = new();
        public List<OrderRequest> MyOrders { get; set; } = new();
        public List<int> CurrentRoleIds { get; set; } = new();
        public bool IsOwner { get; set; }
        public bool IsProviderIndividual { get; set; }
        public bool IsProviderCompany { get; set; }
        public bool IsCustomer { get; set; }

        public static string GetRoleName(int id) => id switch
        {
            1 => "CustomerIndividual",
            2 => "ProviderIndividual",
            3 => "CustomerCompany",
            4 => "ProviderCompany",
            _ => ""
        };

        public static readonly Dictionary<int, string> RoleNames = new()
        {
            { 1, "CustomerIndividual" },
            { 2, "ProviderIndividual" },
            { 3, "CustomerCompany" },
            { 4, "ProviderCompany" }
        };

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                ProfileUser = await _userManager.FindByIdAsync(id);
            }
            else
            {
                ProfileUser = await _userManager.GetUserAsync(User);
            }

            if (ProfileUser == null) return Page();

            var currentUser = await _userManager.GetUserAsync(User);
            IsOwner = currentUser?.Id == ProfileUser.Id;

            var roles = (await _userManager.GetRolesAsync(ProfileUser)).Where(r => r != "Admin").ToList();
            CurrentRoleIds = roles
                .Select(r => RoleNames.FirstOrDefault(x => x.Value == r).Key)
                .Where(k => k > 0)
                .ToList();

            IsProviderIndividual = roles.Contains("ProviderIndividual");
            IsProviderCompany = roles.Contains("ProviderCompany");
            IsCustomer = roles.Contains("CustomerIndividual") || roles.Contains("CustomerCompany");

            MyServices = await _context.Services
                .Where(s => s.ProviderId == ProfileUser.Id)
                .OrderByDescending(s => s.PostedDate)
                .ToListAsync();

            MyOrders = await _context.OrderRequests
                .Include(o => o.Service)
                .Where(o => o.CustomerId == ProfileUser.Id)
                .OrderByDescending(o => o.RequestedDate)
                .ToListAsync();

            if (IsProviderCompany)
            {
                CompanyProfile = await _context.Companies
                    .FirstOrDefaultAsync(c => c.EmployerId == ProfileUser.Id);
            }

            return Page();
        }
    }
}