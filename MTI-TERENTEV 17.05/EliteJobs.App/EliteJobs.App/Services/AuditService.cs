using EliteJobs.App.Data;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EliteJobs.App.Services
{
    public class AuditLog
    {
        [Key] public int Id { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string? userId, string action, object? details = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            };
            _context.Set<AuditLog>().Add(log);
            await _context.SaveChangesAsync();
        }
    }
}