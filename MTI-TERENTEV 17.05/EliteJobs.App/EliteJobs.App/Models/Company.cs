using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string EmployerId { get; set; } = string.Empty;

        [ForeignKey("EmployerId")]
        public ApplicationUser? Employer { get; set; }

        [Required]
        [Display(Name = "Название компании")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание компании")]
        [MaxLength(5000)]
        public string? Description { get; set; }

        [Display(Name = "Логотип")]
        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [Display(Name = "Веб-сайт")]
        [MaxLength(500)]
        public string? Website { get; set; }

        [Display(Name = "Город")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "Адрес")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Ближайшее метро")]
        [MaxLength(200)]
        public string? NearestMetro { get; set; }

        [Display(Name = "Общественный транспорт")]
        [MaxLength(500)]
        public string? PublicTransport { get; set; }

        [Display(Name = "Парковка")]
        public bool HasParking { get; set; }

        [Display(Name = "Количество сотрудников")]
        [MaxLength(50)]
        public string? EmployeesCount { get; set; }

        [Display(Name = "Отрасль")]
        [MaxLength(200)]
        public string? Industry { get; set; }

        [Display(Name = "Тип занятости")]
        [MaxLength(100)]
        public string? EmploymentType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}