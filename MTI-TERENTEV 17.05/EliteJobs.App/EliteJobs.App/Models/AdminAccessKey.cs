using System.ComponentModel.DataAnnotations;

namespace EliteJobs.App.Models
{
    public class AdminAccessKey
    {
        [Key]
        public int Id { get; set; }
        public string KeyHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
    }
}