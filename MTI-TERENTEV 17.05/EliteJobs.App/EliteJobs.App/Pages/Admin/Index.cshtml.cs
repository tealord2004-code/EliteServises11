using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public List<Service> AllServices { get; set; } = new();
        public List<SubscriptionViewModel> Subscriptions { get; set; } = new();
        public List<PaymentViewModel> Payments { get; set; } = new();

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var u in users)
            {
                Users.Add(new UserViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email!,
                    IsActive = u.IsActive,
                    Roles = (await _userManager.GetRolesAsync(u)).ToList()
                });
            }

            AllServices = await _context.Services
                .OrderByDescending(s => s.PostedDate)
                .Take(200)
                .ToListAsync();

            var subs = await _context.Subscriptions.Include(s => s.User).ToListAsync();
            Subscriptions = subs.Select(s => new SubscriptionViewModel
            {
                UserId = s.UserId,
                UserName = $"{s.User?.FirstName} {s.User?.LastName}",
                TierEnum = s.Tier,
                TierDisplay = SubscriptionLimits.DisplayName(s.Tier),
                ServicesUsed = s.ServicesUsed,
                ExpiryDate = s.ExpiryDate
            }).ToList();

            var payments = await _context.PaymentRequests
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();
            Payments = payments.Select(p => new PaymentViewModel
            {
                Id = p.Id,
                UserName = $"{p.User?.FirstName} {p.User?.LastName}",
                TierDisplay = SubscriptionLimits.DisplayName(p.RequestedTier),
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaymentDetails = p.PaymentDetails,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<IActionResult> OnPostToggleBanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteServiceAsync(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetSubscriptionAsync(string userId, SubscriptionTier tier)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (sub == null)
            {
                sub = new Subscription { UserId = userId };
                _context.Subscriptions.Add(sub);
            }
            sub.Tier = tier;
            sub.StartDate = DateTime.UtcNow;
            sub.ExpiryDate = tier == SubscriptionTier.Free ? null : DateTime.UtcNow.AddMonths(1);
            sub.ServicesUsed = 0;
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmPaymentAsync(int paymentId)
        {
            var payment = await _context.PaymentRequests.FindAsync(paymentId);
            if (payment != null && payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Confirmed;
                payment.ProcessedAt = DateTime.UtcNow;

                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == payment.UserId);
                if (sub == null)
                {
                    sub = new Subscription { UserId = payment.UserId };
                    _context.Subscriptions.Add(sub);
                }
                sub.Tier = payment.RequestedTier;
                sub.StartDate = DateTime.UtcNow;
                sub.ExpiryDate = DateTime.UtcNow.AddMonths(1);
                sub.ServicesUsed = 0;

                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectPaymentAsync(int paymentId)
        {
            var payment = await _context.PaymentRequests.FindAsync(paymentId);
            if (payment != null && payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Rejected;
                payment.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class SubscriptionViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string TierDisplay { get; set; } = string.Empty;
        public SubscriptionTier TierEnum { get; set; }
        public int ServicesUsed { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class PaymentViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string TierDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentDetails { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}