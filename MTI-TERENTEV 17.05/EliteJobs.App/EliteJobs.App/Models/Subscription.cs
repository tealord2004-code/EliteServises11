using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }

        public int ServicesUsed { get; set; } = 0;
        public int PromotionsUsed { get; set; } = 0;

        public bool IsActive => Tier == SubscriptionTier.Free ||
            (ExpiryDate.HasValue && ExpiryDate.Value > DateTime.UtcNow);
    }

    public enum SubscriptionTier
    {
        Free,
        Pro,
        Business,
        Premium
    }

    public static class SubscriptionLimits
    {
        public static int MaxServices(SubscriptionTier tier) => tier switch
        {
            SubscriptionTier.Free => 1,
            SubscriptionTier.Pro => 5,
            SubscriptionTier.Business => 20,
            SubscriptionTier.Premium => int.MaxValue,
            _ => 1
        };

        public static int MaxPromotions(SubscriptionTier tier) => tier switch
        {
            SubscriptionTier.Free => 0,
            SubscriptionTier.Pro => 1,
            SubscriptionTier.Business => 3,
            SubscriptionTier.Premium => 5,
            _ => 0
        };

        public static decimal Price(SubscriptionTier tier) => tier switch
        {
            SubscriptionTier.Pro => 500m,
            SubscriptionTier.Business => 1500m,
            SubscriptionTier.Premium => 3000m,
            _ => 0
        };

        public static string DisplayName(SubscriptionTier tier) => tier switch
        {
            SubscriptionTier.Free => "Free",
            SubscriptionTier.Pro => "Pro",
            SubscriptionTier.Business => "Business",
            SubscriptionTier.Premium => "Premium",
            _ => "Free"
        };
    }
}