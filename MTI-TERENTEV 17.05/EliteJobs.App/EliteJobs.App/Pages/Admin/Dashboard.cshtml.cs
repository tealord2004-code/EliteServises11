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
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalServices { get; set; }
        public int PendingPayments { get; set; }

        public string[] ServicesChartLabels { get; set; } = Array.Empty<string>();
        public int[] ServicesChartData { get; set; } = Array.Empty<int>();
        public string[] UsersChartLabels { get; set; } = Array.Empty<string>();
        public int[] UsersChartData { get; set; } = Array.Empty<int>();
        public string[] PaymentsChartLabels { get; set; } = Array.Empty<string>();
        public decimal[] PaymentsChartData { get; set; } = Array.Empty<decimal>();
        public string[] CategoriesLabels { get; set; } = Array.Empty<string>();
        public int[] CategoriesData { get; set; } = Array.Empty<int>();

        public List<RecentUser> RecentUsers { get; set; } = new();
        public List<RecentPayment> RecentPayments { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalUsers = await _context.Users.CountAsync();
            ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            TotalServices = await _context.Services.CountAsync();
            PendingPayments = await _context.PaymentRequests.CountAsync(p => p.Status == PaymentStatus.Pending);

            // Графики за 7 дней
            var days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-i)).Reverse().ToArray();
            ServicesChartLabels = days.Select(d => d.ToString("dd.MM")).ToArray();
            UsersChartLabels = days.Select(d => d.ToString("dd.MM")).ToArray();
            PaymentsChartLabels = days.Select(d => d.ToString("dd.MM")).ToArray();

            ServicesChartData = new int[7];
            UsersChartData = new int[7];
            PaymentsChartData = new decimal[7];

            for (int i = 0; i < 7; i++)
            {
                var day = days[i];
                ServicesChartData[i] = await _context.Services.CountAsync(s => s.PostedDate.Date == day.Date);
                UsersChartData[i] = await _context.Users.CountAsync(u => u.RegisteredDate.Date == day.Date);
                PaymentsChartData[i] = await _context.PaymentRequests
                    .Where(p => p.Status == PaymentStatus.Confirmed && p.ProcessedAt.HasValue && p.ProcessedAt.Value.Date == day.Date)
                    .SumAsync(p => p.Amount);
            }

            // Категории
            var categories = await _context.Services
                .Where(s => s.Category != null)
                .GroupBy(s => s.Category!)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(8)
                .ToListAsync();

            CategoriesLabels = categories.Select(c => c.Category).ToArray();
            CategoriesData = categories.Select(c => c.Count).ToArray();

            // Последние пользователи
            RecentUsers = await _context.Users
                .OrderByDescending(u => u.RegisteredDate)
                .Take(10)
                .Select(u => new RecentUser
                {
                    Name = $"{u.FirstName} {u.LastName}",
                    Date = u.RegisteredDate
                })
                .ToListAsync();

            // Последние платежи
            RecentPayments = await _context.PaymentRequests
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new RecentPayment
                {
                    UserName = $"{p.User!.FirstName} {p.User.LastName}",
                    Tier = SubscriptionLimits.DisplayName(p.RequestedTier),
                    Amount = p.Amount,
                    Status = p.Status.ToString()
                })
                .ToListAsync();
        }
    }

    public class RecentUser
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class RecentPayment
    {
        public string UserName { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}