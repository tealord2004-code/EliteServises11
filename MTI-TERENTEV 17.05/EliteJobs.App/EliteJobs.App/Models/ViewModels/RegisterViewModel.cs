using System.ComponentModel.DataAnnotations;

namespace EliteJobs.App.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, ErrorMessage = "Пароль должен содержать минимум {2} и максимум {1} символов.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифру и спецсимвол")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите роль")]
        [Display(Name = "Роль")]
        public string Role { get; set; } = "Worker";

        [Display(Name = "Компания")]
        [MaxLength(100)]
        public string? Company { get; set; }

        [Display(Name = "Должность")]
        [MaxLength(100)]
        public string? Position { get; set; }
    }
}