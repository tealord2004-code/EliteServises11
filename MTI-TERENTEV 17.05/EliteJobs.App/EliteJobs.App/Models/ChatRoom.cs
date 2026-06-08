using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string User1Id { get; set; } = string.Empty;

        [ForeignKey("User1Id")]
        public ApplicationUser? User1 { get; set; }

        [Required]
        public string User2Id { get; set; } = string.Empty;

        [ForeignKey("User2Id")]
        public ApplicationUser? User2 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessage>? Messages { get; set; }
    }
}