using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;

namespace EliteJobs.App.Pages.Account
{
    public class SupportModel : PageModel
    {
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SupportModel(AuditService auditService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _auditService = auditService;
            _userManager = userManager;
            _context = context;
        }

        public bool Sent { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(string subject, string message, int? serviceId)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? "anonymous";
            await _auditService.LogAsync(userId, $"Support: {subject}", new { message, serviceId });

            if (serviceId.HasValue && subject.Contains("Жалоба"))
            {
                var service = await _context.Services.FindAsync(serviceId.Value);
                if (service != null)
                {
                    await _auditService.LogAsync(userId, "ReportedService", new { serviceId, service.Title });
                }
            }

            Sent = true;
            return Page();
        }
    }
}