using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Employer
{
    [Authorize]
    public class ApplicationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Service? Service { get; set; }
        public List<OrderRequest> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int ServiceId { get; set; }

        public async Task<IActionResult> OnGetAsync(int serviceId)
        {
            Service = await _context.Services.FindAsync(serviceId);
            if (Service == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (Service.ProviderId != user?.Id && !User.IsInRole("Admin"))
                return Forbid();

            Orders = await _context.OrderRequests
                .Include(o => o.Customer)
                .Where(o => o.ServiceId == serviceId)
                .OrderByDescending(o => o.RequestedDate)
                .ToListAsync();

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAcceptAsync(int applicationId)
        {
            var order = await _context.OrderRequests.FindAsync(applicationId);
            if (order != null)
            {
                order.Status = OrderStatus.Accepted;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { serviceId = order?.ServiceId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRejectAsync(int applicationId)
        {
            var order = await _context.OrderRequests.FindAsync(applicationId);
            if (order != null)
            {
                order.Status = OrderStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { serviceId = order?.ServiceId });
        }
    }
}