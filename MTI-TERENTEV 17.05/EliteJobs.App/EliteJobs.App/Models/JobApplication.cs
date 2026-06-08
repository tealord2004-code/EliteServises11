using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class OrderRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Data.Service? Service { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [ForeignKey("CustomerId")]
        public ApplicationUser? Customer { get; set; }

        [Display(Name = "Сообщение")]
        [MaxLength(5000)]
        public string? Message { get; set; }

        [Display(Name = "Дата заявки")]
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Статус")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public int? ChatRoomId { get; set; }

        [ForeignKey("ChatRoomId")]
        public ChatRoom? ChatRoom { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Viewed,
        Accepted,
        Rejected
    }
}