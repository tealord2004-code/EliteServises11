using System.ComponentModel.DataAnnotations;

namespace EliteJobs.App.Models
{
    /// <summary>
    /// Группа справочника (например: "IT", "Транспорт", "Медицина")
    /// </summary>
    public class DirectoryGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Тип справочника: Cities, Professions
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string DirectoryType { get; set; } = string.Empty;

        /// <summary>
        /// Порядок сортировки
        /// </summary>
        public int SortOrder { get; set; }

        public ICollection<DirectoryItem> Items { get; set; } = new List<DirectoryItem>();
    }

    /// <summary>
    /// Элемент справочника (например: "Москва", "C# Developer")
    /// </summary>
    public class DirectoryItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ID группы, к которой относится элемент
        /// </summary>
        public int GroupId { get; set; }

        public DirectoryGroup Group { get; set; } = null!;

        /// <summary>
        /// Дополнительное описание (например, регион для города)
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Порядок сортировки внутри группы
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Активен ли элемент
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}