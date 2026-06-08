using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Chat
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;

        public ListModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public List<ChatViewModel> Chats { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            // Находим все чаты текущего пользователя
            var chatRooms = await _context.ChatRooms
                .Include(c => c.Messages)
                .Where(c => c.User1Id == currentUser.Id || c.User2Id == currentUser.Id)
                .OrderByDescending(c => c.Messages!.Max(m => m.SentAt))
                .ToListAsync();

            foreach (var room in chatRooms)
            {
                var otherUserId = room.User1Id == currentUser.Id ? room.User2Id : room.User1Id;
                var otherUser = await _userManager.FindByIdAsync(otherUserId);

                if (otherUser == null) continue;

                var lastMessage = room.Messages?
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                var unreadCount = room.Messages?
                    .Count(m => m.SenderId != currentUser.Id && !m.IsRead) ?? 0;

                string? decryptedLastMessage = null;
                if (lastMessage != null)
                {
                    try
                    {
                        decryptedLastMessage = _encryptionService.Decrypt(lastMessage.EncryptedContent);
                        if (decryptedLastMessage.Length > 50)
                        {
                            decryptedLastMessage = decryptedLastMessage.Substring(0, 50) + "...";
                        }
                    }
                    catch
                    {
                        decryptedLastMessage = "[Зашифрованное сообщение]";
                    }
                }

                Chats.Add(new ChatViewModel
                {
                    ChatRoomId = room.Id,
                    OtherUserId = otherUserId,
                    OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                    OtherUserCompany = otherUser.Company,
                    LastMessage = decryptedLastMessage,
                    LastMessageTime = lastMessage?.SentAt,
                    UnreadCount = unreadCount
                });
            }

            return Page();
        }
    }

    public class ChatViewModel
    {
        public int ChatRoomId { get; set; }
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserCompany { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}