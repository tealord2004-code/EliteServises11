using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Search
{
    [Authorize(Roles = "Employer,Admin")]
    public class CandidatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CandidatesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Resume> Results { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? City { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EmploymentType { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Resumes
                .Include(r => r.Worker)
                .Where(r => r.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(Query))
            {
                var searchTerm = Query.ToLower();
                query = query.Where(r =>
                    r.DesiredPosition.ToLower().Contains(searchTerm) ||
                    (r.Skills != null && r.Skills.ToLower().Contains(searchTerm)) ||
                    (r.Experience != null && r.Experience.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(City))
            {
                query = query.Where(r => r.City != null && r.City.ToLower().Contains(City.ToLower()));
            }

            if (!string.IsNullOrEmpty(EmploymentType))
            {
                query = query.Where(r => r.EmploymentType != null && r.EmploymentType.Contains(EmploymentType));
            }

            Results = await query
                .OrderByDescending(r => r.UpdatedDate)
                .Take(50)
                .ToListAsync();
        }
    }
}