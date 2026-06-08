using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EliteJobs.App.Services
{
    public class CompetitorAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public CompetitorAnalysisService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EliteServices-Analytics/1.0");
        }

        // Ручной ввод данных о конкурентах (вместо парсинга)
        public async Task<CompetitorData> AddCompetitorDataAsync(string name, string category, int servicesCount,
            decimal avgPrice, string topCity, int providersCount, string? notes = null)
        {
            var data = new CompetitorData
            {
                CompetitorName = name,
                Category = category,
                ServicesCount = servicesCount,
                AvgPrice = avgPrice,
                TopCity = topCity,
                ProvidersCount = providersCount,
                Notes = notes,
                CollectedAt = DateTime.UtcNow
            };
            _context.CompetitorData.Add(data);
            await _context.SaveChangesAsync();
            return data;
        }

        // Получение последних данных по конкурентам
        public async Task<List<CompetitorData>> GetLatestCompetitorDataAsync()
        {
            return await _context.CompetitorData
                .OrderByDescending(c => c.CollectedAt)
                .Take(50)
                .ToListAsync();
        }

        // Генерация бенчмарка
        public async Task<CompetitorBenchmark> GenerateBenchmarkAsync(string competitorName)
        {
            var ourServices = await _context.Services.CountAsync(s => s.IsActive);
            var ourPrices = await _context.Services
                .Where(s => s.IsActive && s.Price != null)
                .Select(s => s.Price)
                .ToListAsync();
            var ourAvgPrice = ourPrices.Any()
                ? (decimal)ourPrices.Select(p => ParsePrice(p!)).Where(p => p > 0).Average()
                : 0;
            var ourProviders = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => s.ProviderId)
                .Distinct()
                .CountAsync();

            var competitorData = await _context.CompetitorData
                .Where(c => c.CompetitorName == competitorName)
                .OrderByDescending(c => c.CollectedAt)
                .FirstOrDefaultAsync();

            var benchmark = new CompetitorBenchmark
            {
                Date = DateTime.UtcNow.Date,
                OurServicesCount = ourServices,
                OurAvgPrice = ourAvgPrice,
                OurProvidersCount = ourProviders,
                CompetitorName = competitorName,
                CompetitorServicesCount = competitorData?.ServicesCount ?? 0,
                CompetitorAvgPrice = competitorData?.AvgPrice ?? 0,
                CompetitorProvidersCount = competitorData?.ProvidersCount ?? 0,
                Insights = GenerateInsights(ourServices, ourAvgPrice, ourProviders, competitorData)
            };

            _context.CompetitorBenchmarks.Add(benchmark);
            await _context.SaveChangesAsync();
            return benchmark;
        }

        // Получение истории бенчмарков
        public async Task<List<CompetitorBenchmark>> GetBenchmarkHistoryAsync(string competitorName, int days = 30)
        {
            return await _context.CompetitorBenchmarks
                .Where(b => b.CompetitorName == competitorName && b.Date >= DateTime.UtcNow.Date.AddDays(-days))
                .OrderByDescending(b => b.Date)
                .ToListAsync();
        }

        private string GenerateInsights(int ourServices, decimal ourAvgPrice, int ourProviders, CompetitorData? competitor)
        {
            if (competitor == null) return "Нет данных о конкуренте.";

            var insights = new List<string>();

            if (ourServices > competitor.ServicesCount)
                insights.Add($"У нас больше услуг: {ourServices} vs {competitor.ServicesCount}");
            else
                insights.Add($"У конкурента больше услуг: {competitor.ServicesCount} vs {ourServices}");

            if (ourAvgPrice < competitor.AvgPrice)
                insights.Add($"Наши цены ниже: {ourAvgPrice:F0}₽ vs {competitor.AvgPrice:F0}₽");
            else
                insights.Add($"Цены конкурента ниже: {competitor.AvgPrice:F0}₽ vs {ourAvgPrice:F0}₽");

            if (ourProviders > competitor.ProvidersCount)
                insights.Add($"У нас больше исполнителей: {ourProviders} vs {competitor.ProvidersCount}");
            else
                insights.Add($"У конкурента больше исполнителей: {competitor.ProvidersCount} vs {ourProviders}");

            return string.Join("; ", insights);
        }

        private static double ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price)) return 0;
            var digits = new string(price.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return double.TryParse(digits, out var result) ? result : 0;
        }
    }
}