using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CompetitorsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly CompetitorDataCollector _collector;

        public CompetitorsModel(ApplicationDbContext context, CompetitorDataCollector collector)
        {
            _context = context;
            _collector = collector;
        }

        public List<CompetitorData> CompetitorData { get; set; } = new();
        public int OurServicesCount { get; set; }
        public decimal OurAvgPrice { get; set; }
        public decimal MarketAvgPrice { get; set; }
        public double MarketAvgServices { get; set; }
        public int SourcesCount { get; set; }
        public int OurProvidersCount { get; set; }

        public string[] CompareLabels { get; set; } = Array.Empty<string>();
        public int[] CompareData { get; set; } = Array.Empty<int>();
        public string[] CategoryLabels { get; set; } = Array.Empty<string>();
        public double[] CategoryData { get; set; } = Array.Empty<double>();
        public string[] PriceLabels { get; set; } = Array.Empty<string>();
        public decimal[] PriceData { get; set; } = Array.Empty<decimal>();
        public string[] ShareLabels { get; set; } = Array.Empty<string>();
        public double[] ShareData { get; set; } = Array.Empty<double>();
        public string[] CityLabels { get; set; } = Array.Empty<string>();
        public int[] CityData { get; set; } = Array.Empty<int>();
        public string[] ProvidersLabels { get; set; } = Array.Empty<string>();
        public int[] ProvidersData { get; set; } = Array.Empty<int>();

        public string[] HistoryLabels { get; set; } = Array.Empty<string>();
        public int[] HistoryOurData { get; set; } = Array.Empty<int>();
        public double[] HistoryMarketData { get; set; } = Array.Empty<double>();

        public List<Recommendation> Recommendations { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
            GenerateRecommendations();
        }

        public async Task<IActionResult> OnPostCollectAllAsync()
        {
            await _collector.CollectAllCategoriesAsync();
            TempData["SuccessMessage"] = "Данные успешно собраны!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGenerateReportAsync()
        {
            var report = await _collector.GenerateMarketReportAsync();
            return File(System.Text.Encoding.UTF8.GetBytes(report), "text/plain", "market-report.txt");
        }

        private async Task LoadDataAsync()
        {
            // Все данные
            CompetitorData = await _context.CompetitorData
                .OrderByDescending(d => d.CollectedAt)
                .ToListAsync();

            SourcesCount = CompetitorData.Select(d => d.CompetitorName).Distinct().Count();
            OurServicesCount = await _context.Services.CountAsync(s => s.IsActive);
            OurProvidersCount = await _context.Services.Where(s => s.IsActive).Select(s => s.ProviderId).Distinct().CountAsync();

            var prices = await _context.Services.Where(s => s.IsActive && s.Price != null).Select(s => s.Price).ToListAsync();
            OurAvgPrice = prices.Any() ? (decimal)prices.Select(p => ParsePrice(p!)).Where(p => p > 0).Average() : 0;

            var compPrices = CompetitorData.Where(c => c.AvgPrice > 0).Select(c => c.AvgPrice).ToList();
            MarketAvgPrice = compPrices.Any() ? compPrices.Average() : 0;
            MarketAvgServices = CompetitorData.Any() ? CompetitorData.Average(c => c.ServicesCount) : 0;

            // Мы vs Конкуренты
            var sourceGroups = CompetitorData.GroupBy(c => c.CompetitorName)
                .Select(g => new { Name = g.Key, Total = g.Sum(c => c.ServicesCount) })
                .OrderByDescending(g => g.Total).Take(5).ToList();

            var labels = new List<string> { "Наша платформа" };
            var data = new List<int> { OurServicesCount };
            labels.AddRange(sourceGroups.Select(g => g.Name));
            data.AddRange(sourceGroups.Select(g => g.Total));
            CompareLabels = labels.ToArray();
            CompareData = data.ToArray();

            // Категории
            var cats = CompetitorData.GroupBy(c => c.Category)
                .Select(g => new { Cat = g.Key, Count = g.Sum(c => c.ServicesCount) })
                .OrderByDescending(g => g.Count).Take(8).ToList();
            CategoryLabels = cats.Select(c => c.Cat).ToArray();
            CategoryData = cats.Select(c => (double)c.Count).ToArray();

            // Цены по категориям
            var priceCats = CompetitorData.Where(c => c.AvgPrice > 0)
                .GroupBy(c => c.Category)
                .Select(g => new { Cat = g.Key, Price = g.Average(c => c.AvgPrice) })
                .OrderByDescending(g => g.Price).Take(8).ToList();
            PriceLabels = priceCats.Select(p => p.Cat).ToArray();
            PriceData = priceCats.Select(p => p.Price).ToArray();

            // Доля рынка
            var share = CompetitorData.GroupBy(c => c.CompetitorName)
                .Select(g => new { Name = g.Key, Total = g.Sum(c => c.ServicesCount) })
                .OrderByDescending(g => g.Total).Take(5).ToList();
            ShareLabels = share.Select(s => s.Name).ToArray();
            ShareData = share.Select(s => (double)s.Total).ToArray();

            // Города
            var cities = CompetitorData.Where(c => !string.IsNullOrEmpty(c.TopCity) && c.TopCity != "-")
                .GroupBy(c => c.TopCity!)
                .Select(g => new { City = g.Key, Count = g.Sum(c => c.ServicesCount) })
                .OrderByDescending(g => g.Count).Take(6).ToList();
            CityLabels = cities.Select(c => c.City).ToArray();
            CityData = cities.Select(c => c.Count).ToArray();

            // Исполнители по источникам
            var providersBySource = CompetitorData.GroupBy(c => c.CompetitorName)
                .Select(g => new { Name = g.Key, Total = g.Sum(c => c.ProvidersCount) })
                .OrderByDescending(g => g.Total).Take(6).ToList();
            ProvidersLabels = providersBySource.Select(p => p.Name).ToArray();
            ProvidersData = providersBySource.Select(p => p.Total).ToArray();

            // Динамика за 14 дней
            var days = Enumerable.Range(0, 14).Select(i => DateTime.UtcNow.Date.AddDays(-i)).Reverse().ToList();
            HistoryLabels = days.Select(d => d.ToString("dd.MM")).ToArray();
            HistoryOurData = new int[14];
            HistoryMarketData = new double[14];

            for (int i = 0; i < 14; i++)
            {
                var day = days[i];
                HistoryOurData[i] = await _context.Services.CountAsync(s => s.PostedDate.Date <= day.Date && s.IsActive);
                var dayData = CompetitorData.Where(d => d.CollectedAt.Date == day.Date).ToList();
                HistoryMarketData[i] = dayData.Any() ? dayData.Average(d => d.ServicesCount) : 0;
            }
        }

        private void GenerateRecommendations()
        {
            Recommendations = new List<Recommendation>();

            if (OurServicesCount > MarketAvgServices && MarketAvgServices > 0)
                Recommendations.Add(new Recommendation { Type = "success", Title = "Мы лидируем по количеству услуг", Description = $"У нас {OurServicesCount} услуг против {MarketAvgServices:N0} в среднем у конкурентов (+{OurServicesCount - MarketAvgServices:N0})." });
            else if (MarketAvgServices > 0)
                Recommendations.Add(new Recommendation { Type = "warning", Title = "Нужно больше услуг", Description = $"У нас {OurServicesCount}, у конкурентов {MarketAvgServices:N0}. Отстаём на {MarketAvgServices - OurServicesCount:N0}." });

            if (OurAvgPrice < MarketAvgPrice && MarketAvgPrice > 0)
                Recommendations.Add(new Recommendation { Type = "success", Title = "Наши цены привлекательнее", Description = $"Средняя цена: {OurAvgPrice:N0} ₽ против {MarketAvgPrice:N0} ₽ на рынке." });
            else if (OurAvgPrice > MarketAvgPrice && MarketAvgPrice > 0)
                Recommendations.Add(new Recommendation { Type = "warning", Title = "Цены выше рыночных", Description = $"У нас {OurAvgPrice:N0} ₽, на рынке {MarketAvgPrice:N0} ₽." });

            if (CategoryLabels.Any())
                Recommendations.Add(new Recommendation { Type = "info", Title = $"Топ категория: {CategoryLabels[0]}", Description = $"{CategoryData[0]:N0} предложений. Развивайте это направление." });

            if (CityLabels.Any())
                Recommendations.Add(new Recommendation { Type = "info", Title = $"Максимум конкуренции: {CityLabels[0]}", Description = $"Город с наибольшим числом предложений." });

            if (OurProvidersCount < ProvidersData.FirstOrDefault() && ProvidersData.Any())
                Recommendations.Add(new Recommendation { Type = "warning", Title = "Мало исполнителей", Description = $"У нас {OurProvidersCount}, у лидера {ProvidersData.First()}. Привлекайте специалистов." });

            Recommendations.Add(new Recommendation { Type = "info", Title = $"{SourcesCount} источников данных", Description = "Нажмите «Собрать данные» для обновления." });
        }

        private static double ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price)) return 0;
            var digits = new string(price.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return double.TryParse(digits, out var result) ? result : 0;
        }
    }

    public class Recommendation
    {
        public string Type { get; set; } = "info";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}