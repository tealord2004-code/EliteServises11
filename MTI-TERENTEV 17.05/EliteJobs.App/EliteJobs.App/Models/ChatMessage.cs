using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatRoomId { get; set; }

        [ForeignKey("ChatRoomId")]
        public ChatRoom? ChatRoom { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public ApplicationUser? Sender { get; set; }

        // Сообщение хранится в зашифрованном виде
        [Required]
        public string EncryptedContent { get; set; } = string.Empty;

        // Хеш для проверки целостности
        [Required]
        [MaxLength(64)]
        public string ContentHash { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Прочитано")]
        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }
    }
}