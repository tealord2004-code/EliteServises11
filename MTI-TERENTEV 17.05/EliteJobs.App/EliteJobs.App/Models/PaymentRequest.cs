using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class PaymentRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public SubscriptionTier RequestedTier { get; set; }

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        public string? AdminNotes { get; set; }

        // "Способ оплаты" и "детали"
        [MaxLength(200)]
        public string? PaymentMethod { get; set; }

        [MaxLength(500)]
        public string? PaymentDetails { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Confirmed,
        Rejected,
        Expired
    }
}