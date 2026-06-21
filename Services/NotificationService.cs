using JiSaveSacco.API.Data;
using JiSaveSacco.API.Models;

namespace JiSaveSacco.API.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Send(int memberId, string title, string message)
        {
            var notification = new Notification
            {
                MemberId = memberId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}