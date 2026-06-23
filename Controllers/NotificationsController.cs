using JiSaveSacco.API.Data;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IdentityService _identity;

        public NotificationsController(
            AppDbContext context,
            IdentityService identity)
        {
            _context = context;
            _identity = identity;
        }

        // =========================
        // GET MY NOTIFICATIONS
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.MemberId == memberId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        // =========================
        // MARK AS READ
        // =========================
        [Authorize(Roles = "Member")]
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.NotificationId == id &&
                    n.MemberId == memberId);

            if (notification == null)
                return NotFound("Notification not found");

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok("Notification marked as read");
        }

        // =========================
        // UNREAD COUNT
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized();

            var count = await _context.Notifications
                .CountAsync(n =>
                    n.MemberId == memberId &&
                    !n.IsRead);

            return Ok(new
            {
                unreadNotifications = count
            });
        }
    }
}