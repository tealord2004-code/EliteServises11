using EliteJobs.App.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteJobs.App.Data
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название услуги обязательно")]
        [Display(Name = "Название услуги")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Категория")]
        [MaxLength(100)]
        public string? Category { get; set; }

        [Display(Name = "Стоимость")]
        [MaxLength(100)]
        public string? Price { get; set; }

        [Display(Name = "Город")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "Описание")]
        [MaxLength(5000)]
        public string? Description { get; set; }

        [Display(Name = "Исполнитель")]
        [MaxLength(200)]
        public string? ProviderName { get; set; }

        [Display(Name = "Тип исполнителя")]
        [MaxLength(50)]
        public string? ProviderType { get; set; } // "Физ. лицо" или "Юр. лицо"

        [Display(Name = "Контакты")]
        [MaxLength(200)]
        public string? Contacts { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;

        // Связь с пользователем
        public string? ProviderId { get; set; }

        [ForeignKey("ProviderId")]
        public ApplicationUser? Provider { get; set; }
    }
}