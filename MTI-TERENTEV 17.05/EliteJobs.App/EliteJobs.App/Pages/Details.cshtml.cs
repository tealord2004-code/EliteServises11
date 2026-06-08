using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Service? Service { get; set; }
        public bool IsOwner { get; set; }
        public int OrderCount { get; set; }
        public bool AlreadyOrdered { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Service = await _context.Services.FirstOrDefaultAsync(s => s.Id == Id);
            if (Service == null) return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                IsOwner = user?.Id == Service.ProviderId;

                if (IsOwner)
                    OrderCount = await _context.OrderRequests.CountAsync(o => o.ServiceId == Id);
                else
                    AlreadyOrdered = await _context.OrderRequests.AnyAsync(o => o.ServiceId == Id && o.CustomerId == user!.Id);
            }

            return Page();
        }
    }
}