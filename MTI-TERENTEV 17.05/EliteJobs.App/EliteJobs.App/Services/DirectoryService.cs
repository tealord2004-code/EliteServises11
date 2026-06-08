using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EliteJobs.App.Services
{
    public interface IDirectoryService
    {
        Task<List<DirectoryGroup>> GetCityGroupsAsync();
        Task<List<DirectoryGroup>> GetProfessionGroupsAsync();
        Task<List<DirectoryItem>> SearchCitiesAsync(string query, int maxResults = 10);
        Task<List<DirectoryItem>> SearchProfessionsAsync(string query, int maxResults = 10);
    }

    public class DirectoryService : IDirectoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        public DirectoryService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<DirectoryGroup>> GetCityGroupsAsync()
        {
            const string cacheKey = "CityGroups";
            if (_cache.TryGetValue(cacheKey, out List<DirectoryGroup>? cached))
                return cached ?? new List<DirectoryGroup>();

            var groups = await _context.DirectoryGroups
                .Include(g => g.Items.Where(i => i.IsActive))
                .Where(g => g.DirectoryType == "Cities")
                .OrderBy(g => g.SortOrder)
                .ToListAsync();

            _cache.Set(cacheKey, groups, _cacheDuration);
            return groups;
        }

        public async Task<List<DirectoryGroup>> GetProfessionGroupsAsync()
        {
            const string cacheKey = "ProfessionGroups";
            if (_cache.TryGetValue(cacheKey, out List<DirectoryGroup>? cached))
                return cached ?? new List<DirectoryGroup>();

            var groups = await _context.DirectoryGroups
                .Include(g => g.Items.Where(i => i.IsActive))
                .Where(g => g.DirectoryType == "Professions")
                .OrderBy(g => g.SortOrder)
                .ToListAsync();

            _cache.Set(cacheKey, groups, _cacheDuration);
            return groups;
        }

        public async Task<List<DirectoryItem>> SearchCitiesAsync(string query, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
                return new List<DirectoryItem>();

            var lowerQuery = query.ToLower();
            return await _context.DirectoryItems
                .Include(i => i.Group)
                .Where(i => i.IsActive && i.Group.DirectoryType == "Cities" &&
                            i.Name.ToLower().Contains(lowerQuery))
                .OrderBy(i => i.SortOrder)
                .Take(maxResults)
                .ToListAsync();
        }

        public async Task<List<DirectoryItem>> SearchProfessionsAsync(string query, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
                return new List<DirectoryItem>();

            var lowerQuery = query.ToLower();
            return await _context.DirectoryItems
                .Include(i => i.Group)
                .Where(i => i.IsActive && i.Group.DirectoryType == "Professions" &&
                            i.Name.ToLower().Contains(lowerQuery))
                .OrderBy(i => i.SortOrder)
                .Take(maxResults)
                .ToListAsync();
        }
    }
}