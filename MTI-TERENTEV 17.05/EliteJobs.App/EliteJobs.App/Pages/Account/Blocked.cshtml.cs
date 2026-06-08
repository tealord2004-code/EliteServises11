using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EliteJobs.App.Pages.Account
{
    public class BlockedModel : PageModel
    {
        public string Reason { get; set; } = "Ваш аккаунт заблокирован за подозрительную активность.";

        public void OnGet(string? reason)
        {
            if (!string.IsNullOrEmpty(reason))
                Reason = reason switch
                {
                    "spam" => "Обнаружена спам-активность. Аккаунт временно заблокирован.",
                    "banned" => "Аккаунт заблокирован администратором.",
                    _ => "Аккаунт заблокирован."
                };
        }
    }
}