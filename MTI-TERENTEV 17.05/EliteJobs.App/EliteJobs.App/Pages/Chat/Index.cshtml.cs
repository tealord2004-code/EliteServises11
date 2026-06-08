using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EliteJobs.App.Pages.Chat
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public ChatRoom? ChatRoom { get; set; }
        public ApplicationUser? OtherUser { get; set; }
        public string? OtherUserId { get; set; }
        public string? CurrentUserId { get; set; }
        public List<MessageViewModel> Messages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            CurrentUserId = currentUser.Id;
            OtherUserId = userId;

            // Нельзя чатиться с самим собой
            if (currentUser.Id == userId)
            {
                return RedirectToPage("/Index");
            }

            // Ищем другого пользователя
            OtherUser = await _userManager.FindByIdAsync(userId);
            if (OtherUser == null)
                return NotFound();

            // Ищем или создаём чат-комнату
            var userIds = new[] { currentUser.Id, userId }.OrderBy(id => id).ToArray();

            ChatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.User1Id == userIds[0] && c.User2Id == userIds[1]);

            if (ChatRoom == null)
            {
                ChatRoom = new ChatRoom
                {
                    User1Id = userIds[0],
                    User2Id = userIds[1],
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatRooms.Add(ChatRoom);
                await _context.SaveChangesAsync();
            }

            // Загружаем сообщения
            var messages = await _context.ChatMessages
                .Where(m => m.ChatRoomId == ChatRoom.Id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Расшифровываем сообщения и проверяем целостность
            foreach (var message in messages)
            {
                var decryptedContent = _encryptionService.Decrypt(message.EncryptedContent);
                var computedHash = ComputeHash(decryptedContent);

                Messages.Add(new MessageViewModel
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    DecryptedContent = WebUtility.HtmlEncode(decryptedContent),
                    SentAt = message.SentAt,
                    IsRead = message.IsRead,
                    IsIntegrityValid = computedHash == message.ContentHash
                });
            }

            // Отмечаем сообщения как прочитанные
            var unreadMessages = messages
                .Where(m => m.SenderId != currentUser.Id && !m.IsRead)
                .ToList();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(string otherUserId, string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return BadRequest("Сообщение не может быть пустым.");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            // Санитизация ввода
            messageContent = WebUtility.HtmlEncode(messageContent.Trim());

            // Ищем или создаём чат-комнату
            var userIds = new[] { currentUser.Id, otherUserId }.OrderBy(id => id).ToArray();

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.User1Id == userIds[0] && c.User2Id == userIds[1]);

            if (chatRoom == null)
            {
                chatRoom = new ChatRoom
                {
                    User1Id = userIds[0],
                    User2Id = userIds[1],
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatRooms.Add(chatRoom);
                await _context.SaveChangesAsync();
            }

            // Шифруем сообщение
            var encryptedContent = _encryptionService.Encrypt(messageContent);
            var contentHash = ComputeHash(messageContent);

            var chatMessage = new ChatMessage
            {
                ChatRoomId = chatRoom.Id,
                SenderId = currentUser.Id,
                EncryptedContent = encryptedContent,
                ContentHash = contentHash,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Message sent from {currentUser.Id} to {otherUserId}");

            return RedirectToPage(new { userId = otherUserId });
        }

        private string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    public class MessageViewModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string DecryptedContent { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsIntegrityValid { get; set; }
    }
}