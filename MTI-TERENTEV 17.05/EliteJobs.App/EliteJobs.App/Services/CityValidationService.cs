using EliteJobs.App.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Services
{
    public interface ICityValidationService
    {
        Task<bool> IsValidCityAsync(string cityName);
        Task<string?> GetCityMetroAsync(string cityName);
    }

    public class CityValidationService : ICityValidationService
    {
        private readonly ApplicationDbContext _context;

        public CityValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsValidCityAsync(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName)) return false;
            return await _context.DirectoryItems
                .AnyAsync(i => i.IsActive &&
                               i.Group.DirectoryType == "Cities" &&
                               i.Name.ToLower() == cityName.ToLower());
        }

        public async Task<string?> GetCityMetroAsync(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName)) return null;

            // Города с метро
            var citiesWithMetro = new[] { "Москва", "Санкт-Петербург", "Казань", "Нижний Новгород",
                                          "Новосибирск", "Екатеринбург", "Самара", "Минск" };

            if (citiesWithMetro.Any(c => c.Equals(cityName, StringComparison.OrdinalIgnoreCase)))
            {
                return "metro";
            }

            return null; // Нет метро — нужно указать остановку
        }
    }
}