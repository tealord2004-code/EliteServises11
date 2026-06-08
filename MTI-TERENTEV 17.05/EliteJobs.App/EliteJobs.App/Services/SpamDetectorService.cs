using EliteJobs.App.Data;
using EliteJobs.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Services
{
    public class SpamDetectorService
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> _bannedWords = new()
        {
            "casino", "gambling", "porn", "xxx", "sex", "drugs", "наркотик",
            "казино", "порно", "секс", "ставки", "bet", "криптовалюта",
            "bitcoin", "btc", "заработок без вложений", "обнал", "обналичка"
        };

        public SpamDetectorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool ContainsBannedContent(string title, string description)
        {
            var text = (title + " " + description).ToLower();
            return _bannedWords.Any(w => text.Contains(w));
        }

        public async Task<bool> IsSpamBehaviorAsync(string userId)
        {
            var recentServices = await _context.Services
                .CountAsync(s => s.ProviderId == userId && s.PostedDate >= DateTime.UtcNow.AddMinutes(-10));

            if (recentServices >= 5) return true;

            var todayServices = await _context.Services
                .CountAsync(s => s.ProviderId == userId && s.PostedDate.Date == DateTime.UtcNow.Date);

            if (todayServices >= 20) return true;

            var duplicateTitles = await _context.Services
                .Where(s => s.ProviderId == userId)
                .GroupBy(s => s.Title.ToLower())
                .Select(g => g.Count())
                .AnyAsync(c => c >= 3);

            return duplicateTitles;
        }

        public async Task AutoBlockIfNeededAsync(string userId)
        {
            var violations = await _context.AuditLogs
                .CountAsync(a => a.UserId == userId && a.Action == "SpamViolation"
                    && a.Timestamp >= DateTime.UtcNow.AddDays(-1));

            if (violations >= 3)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !user.IsActive) return;

                if (user != null)
                {
                    user.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}