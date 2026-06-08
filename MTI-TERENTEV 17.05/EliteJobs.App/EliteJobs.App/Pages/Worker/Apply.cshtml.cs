using EliteJobs.App.Data;
using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EliteJobs.App.Pages.Worker
{
    [Authorize]
    public class ApplyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<ApplyModel> _logger;

        public ApplyModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService,
            ILogger<ApplyModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public Service? Service { get; set; }
        public bool AlreadyOrdered { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ServiceId { get; set; }

        public async Task<IActionResult> OnGetAsync(int serviceId)
        {
            ServiceId = serviceId;
            Service = await _context.Services.FindAsync(serviceId);
            if (Service == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                AlreadyOrdered = await _context.OrderRequests
                    .AnyAsync(o => o.ServiceId == serviceId && o.CustomerId == user.Id);
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(string message)
        {
            Service = await _context.Services.FindAsync(ServiceId);
            if (Service == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existing = await _context.OrderRequests
                .FirstOrDefaultAsync(o => o.ServiceId == ServiceId && o.CustomerId == user.Id);

            if (existing != null)
            {
                TempData["ErrorMessage"] = "Order already exists";
                return RedirectToPage("/Details", new { id = ServiceId });
            }

            // Создаём чат
            var userIds = new[] { user.Id, Service.ProviderId ?? "" }
                .Where(id => !string.IsNullOrEmpty(id))
                .OrderBy(id => id)
                .ToArray();

            ChatRoom? chatRoom = null;
            if (userIds.Length == 2)
            {
                chatRoom = await _context.ChatRooms
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
            }

            var order = new OrderRequest
            {
                ServiceId = ServiceId,
                CustomerId = user.Id,
                Message = System.Net.WebUtility.HtmlEncode(message ?? ""),
                RequestedDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                ChatRoomId = chatRoom?.Id
            };

            _context.OrderRequests.Add(order);

            if (chatRoom != null && !string.IsNullOrEmpty(message))
            {
                var hash = ComputeHash(message);
                _context.ChatMessages.Add(new ChatMessage
                {
                    ChatRoomId = chatRoom.Id,
                    SenderId = user.Id,
                    EncryptedContent = _encryptionService.Encrypt(message),
                    ContentHash = hash,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"User {user.Email} ordered service {ServiceId}");
            TempData["SuccessMessage"] = "Order sent!";
            return RedirectToPage("/Index");
        }

        private static string ComputeHash(string c)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(c))).ToLower();
        }
    }
}