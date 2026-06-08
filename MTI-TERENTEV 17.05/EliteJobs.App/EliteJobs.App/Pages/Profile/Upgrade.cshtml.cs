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
    public class UpgradeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UpgradeModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<PlanViewModel> Plans { get; set; } = new();
        public List<PaymentHistoryViewModel> PaymentHistory { get; set; } = new();
        public string? Message { get; set; }

        public async Task OnGetAsync(string? limit)
        {
            var user = await _userManager.GetUserAsync(User);
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            var tier = sub?.Tier ?? SubscriptionTier.Free;
            var isActive = sub?.IsActive ?? true;

            if (!isActive && tier != SubscriptionTier.Free)
            {
                tier = SubscriptionTier.Free;
            }

            if (limit == "services")
                Message = "You've reached your service limit. Upgrade to post more!";

            Plans = new List<PlanViewModel>
            {
                new() { Name = "Free", Tier = "Free", Price = 0, MaxServices = 1, MaxPromotions = 0, IsCurrent = tier == SubscriptionTier.Free },
                new() { Name = "Pro", Tier = "Pro", Price = 500, MaxServices = 5, MaxPromotions = 1, IsCurrent = tier == SubscriptionTier.Pro },
                new() { Name = "Business", Tier = "Business", Price = 1500, MaxServices = 20, MaxPromotions = 3, IsCurrent = tier == SubscriptionTier.Business },
                new() { Name = "Premium", Tier = "Premium", Price = 3000, MaxServices = 999, MaxPromotions = 5, IsCurrent = tier == SubscriptionTier.Premium },
            };

            PaymentHistory = await _context.PaymentRequests
                .Where(p => p.UserId == user!.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentHistoryViewModel
                {
                    TierDisplay = SubscriptionLimits.DisplayName(p.RequestedTier),
                    Amount = p.Amount,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }

        // Бесплатное переключение на Free
        public async Task<IActionResult> OnPostUpgradeAsync(SubscriptionTier tier)
        {
            if (tier != SubscriptionTier.Free)
                return RedirectToPage();

            var user = await _userManager.GetUserAsync(User);
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            if (sub != null)
            {
                sub.Tier = SubscriptionTier.Free;
                sub.ExpiryDate = null;
                sub.ServicesUsed = 0;
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Switched to Free plan.";
            return RedirectToPage();
        }

        // Запрос на платёж
        public async Task<IActionResult> OnPostRequestPaymentAsync(SubscriptionTier tier, string paymentMethod, string paymentDetails)
        {
            if (tier == SubscriptionTier.Free)
                return RedirectToPage();

            var user = await _userManager.GetUserAsync(User);

            // Проверяем, нет ли уже pending запроса
            var existing = await _context.PaymentRequests
                .AnyAsync(p => p.UserId == user!.Id && p.Status == PaymentStatus.Pending);
            if (existing)
            {
                TempData["ErrorMessage"] = "You already have a pending payment request.";
                return RedirectToPage();
            }

            var request = new PaymentRequest
            {
                UserId = user!.Id,
                RequestedTier = tier,
                Amount = SubscriptionLimits.Price(tier),
                PaymentMethod = paymentMethod,
                PaymentDetails = paymentDetails,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Payment request for {SubscriptionLimits.DisplayName(tier)} sent! Admin will verify.";
            return RedirectToPage();
        }
    }

    public class PlanViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxServices { get; set; }
        public int MaxPromotions { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class PaymentHistoryViewModel
    {
        public string TierDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}