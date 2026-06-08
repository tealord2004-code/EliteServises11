using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EliteJobs.App.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(50)]
        public string? MaritalStatus { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber2 { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(100)]
        public string? Citizenship { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [MaxLength(200)]
        public string? Position { get; set; }

        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<Data.Service>? PostedServices { get; set; }
    }
}