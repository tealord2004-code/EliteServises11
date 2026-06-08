using System.ComponentModel.DataAnnotations;

namespace EliteJobs.App.Models
{
    public class CompetitorData
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string CompetitorName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Category { get; set; }

        public int ServicesCount { get; set; }

        public decimal AvgPrice { get; set; }

        [MaxLength(100)]
        public string? TopCity { get; set; }

        public int ProvidersCount { get; set; }

        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class CompetitorBenchmark
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        public int OurServicesCount { get; set; }
        public decimal OurAvgPrice { get; set; }
        public int OurProvidersCount { get; set; }

        [MaxLength(100)]
        public string? CompetitorName { get; set; }
        public int CompetitorServicesCount { get; set; }
        public decimal CompetitorAvgPrice { get; set; }
        public int CompetitorProvidersCount { get; set; }

        [MaxLength(500)]
        public string? Insights { get; set; }
    }
}