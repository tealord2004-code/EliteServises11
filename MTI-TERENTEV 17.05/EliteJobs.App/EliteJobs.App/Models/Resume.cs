using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class Resume
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string WorkerId { get; set; } = string.Empty;

        [ForeignKey("WorkerId")]
        public ApplicationUser? Worker { get; set; }

        [Required]
        [Display(Name = "Желаемая должность")]
        [MaxLength(200)]
        public string DesiredPosition { get; set; } = string.Empty;

        [Display(Name = "Ожидаемая зарплата")]
        [MaxLength(50)]
        public string? DesiredSalary { get; set; }

        [Display(Name = "Тип занятости")]
        [MaxLength(100)]
        public string? EmploymentType { get; set; }

        [Display(Name = "График работы")]
        [MaxLength(100)]
        public string? WorkSchedule { get; set; }

        [Display(Name = "Опыт работы")]
        [MaxLength(5000)]
        public string? Experience { get; set; }

        [Display(Name = "Ключевые навыки")]
        [MaxLength(1000)]
        public string? Skills { get; set; }

        [Display(Name = "Образование")]
        [MaxLength(2000)]
        public string? Education { get; set; }

        [Display(Name = "Знание языков")]
        [MaxLength(500)]
        public string? Languages { get; set; }

        [Display(Name = "О себе")]
        [MaxLength(5000)]
        public string? About { get; set; }

        [Display(Name = "Город")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "Готов к переезду")]
        public bool ReadyToRelocate { get; set; } = false;

        [Display(Name = "Готов к удалённой работе")]
        public bool ReadyForRemote { get; set; } = true;

        [Display(Name = "Наличие авто")]
        public bool HasCar { get; set; }

        [Display(Name = "Водительские права")]
        [MaxLength(50)]
        public string? DrivingLicense { get; set; }

        [Display(Name = "Активно")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}