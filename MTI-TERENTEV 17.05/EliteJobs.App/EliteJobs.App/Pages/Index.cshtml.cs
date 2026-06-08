using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Service> Services { get; set; } = new List<Service>();
        public IList<Service> Recommended { get; set; } = new List<Service>();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? City { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ProviderType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Services.Where(s => s.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(Search))
            {
                var term = Search.ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(term) ||
                    (s.Description != null && s.Description.ToLower().Contains(term)));
            }
            if (!string.IsNullOrEmpty(City))
                query = query.Where(s => s.City != null && s.City.ToLower().Contains(City.ToLower()));
            if (!string.IsNullOrEmpty(Category))
                query = query.Where(s => s.Category == Category);
            if (!string.IsNullOrEmpty(ProviderType))
                query = query.Where(s => s.ProviderType == ProviderType);

            query = Sort switch
            {
                "cheapest" => query.OrderBy(s => s.Price),
                "expensive" => query.OrderByDescending(s => s.Price),
                _ => query.OrderByDescending(s => s.PostedDate)
            };

            Services = await query.Take(50).ToListAsync();

            // Рекомендации на основе города
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (!string.IsNullOrEmpty(user?.City))
                {
                    Recommended = await _context.Services
                        .Where(s => s.IsActive && s.City == user.City && s.ProviderId != user.Id)
                        .OrderByDescending(s => s.PostedDate)
                        .Take(4)
                        .ToListAsync();
                }
            }
        }
    }
}