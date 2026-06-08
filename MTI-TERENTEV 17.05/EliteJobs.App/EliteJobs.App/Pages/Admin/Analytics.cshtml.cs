using EliteJobs.App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EliteJobs.App.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsModel(ApplicationDbContext context) { _context = context; }

        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }
        public decimal AvgPrice { get; set; }
        public string TopCity { get; set; } = "-";
        public int IndividualCount { get; set; }
        public int CompanyCount { get; set; }

        public string[] CatLabels { get; set; } = Array.Empty<string>();
        public int[] CatData { get; set; } = Array.Empty<int>();
        public string[] CityLabels { get; set; } = Array.Empty<string>();
        public int[] CityData { get; set; } = Array.Empty<int>();
        public string[] DailyLabels { get; set; } = Array.Empty<string>();
        public int[] DailyData { get; set; } = Array.Empty<int>();

        public List<ReportRow> Report { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalServices = await _context.Services.CountAsync();
            ActiveServices = await _context.Services.CountAsync(s => s.IsActive);

            // Средняя цена
            var prices = await _context.Services
                .Where(s => s.Price != null)
                .Select(s => s.Price)
                .ToListAsync();

            var parsedPrices = prices
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => ParsePrice(p!))
                .Where(p => p > 0)
                .ToList();

            AvgPrice = parsedPrices.Any() ? (decimal)parsedPrices.Average() : 0;

            // Топ город
            var cities = await _context.Services
                .Where(s => s.City != null)
                .Select(s => s.City)
                .ToListAsync();

            TopCity = cities
                .Where(c => !string.IsNullOrEmpty(c))
                .GroupBy(c => c!)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "-";

            // Типы
            IndividualCount = await _context.Services.CountAsync(s => s.ProviderType == "Физ. лицо");
            CompanyCount = await _context.Services.CountAsync(s => s.ProviderType == "Юр. лицо");

            // Категории
            var catGroups = await _context.Services
                .Where(s => s.Category != null)
                .Select(s => s.Category)
                .ToListAsync();

            var catData = catGroups
                .Where(c => !string.IsNullOrEmpty(c))
                .GroupBy(c => c!)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            CatLabels = catData.Select(c => c.Key).ToArray();
            CatData = catData.Select(c => c.Count()).ToArray();

            // Города для графика
            var cityGroups = cities
                .Where(c => !string.IsNullOrEmpty(c))
                .GroupBy(c => c!)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            CityLabels = cityGroups.Select(c => c.Key).ToArray();
            CityData = cityGroups.Select(c => c.Count()).ToArray();

            // По дням
            var days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Reverse()
                .ToList();

            DailyLabels = days.Select(d => d.ToString("dd.MM")).ToArray();
            DailyData = new int[7];

            var services = await _context.Services
                .Select(s => new { s.PostedDate })
                .ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                var day = days[i];
                DailyData[i] = services.Count(s => s.PostedDate.Date == day.Date);
            }

            // Отчёт
            var reportData = await _context.Services
                .Where(s => s.Category != null)
                .Select(s => new { s.Category, s.Price, s.IsActive })
                .ToListAsync();

            Report = reportData
                .Where(r => !string.IsNullOrEmpty(r.Category))
                .GroupBy(r => r.Category!)
                .Select(g => new ReportRow
                {
                    Category = g.Key,
                    Count = g.Count(),
                    AvgPrice = (decimal)g.Average(r => ParsePrice(r.Price ?? "")),
                    ActiveCount = g.Count(r => r.IsActive)
                })
                .OrderByDescending(r => r.Count)
                .ToList();
        }

        private static double ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price)) return 0;
            var digits = new string(price.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return double.TryParse(digits, out var result) ? result : 0;
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            var data = await _context.Services
                .Where(s => s.Category != null)
                .GroupBy(s => s.Category!)
                .Select(g => $"{g.Key},{g.Count()},{g.Count(s => s.IsActive)}")
                .ToListAsync();
            var csv = "Категория,Всего,Активных\n" + string.Join("\n", data);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "analytics.csv");
        }
    }

    public class ReportRow
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal AvgPrice { get; set; }
        public int ActiveCount { get; set; }
    }
}