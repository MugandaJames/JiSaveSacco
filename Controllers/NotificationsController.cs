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

        // =========================================================
        // ADMIN / STAFF: LIVE ACTION FEED (DYNAMIC WORKFLOW QUEUE)
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            // 1. Compile pending user accounts from Members table using your exact CreatedAt property
            var pendingMembers = await _context.Members
                .Where(m => m.Status == "Pending")
                .Select(m => new
                {
                    Type = "Registration",
                    Message = $"Pending Onboarding: {m.FirstName} {m.LastName} is awaiting KYC approval.",
                    Timestamp = m.CreatedAt // FIXED: Using your exact model property
                })
                .ToListAsync();

            // 2. Compile unapproved loan requests from Loans table
            var pendingLoans = await _context.Loans
                .Include(l => l.Member)
                .Where(l => l.Status == "Pending")
                .Select(l => new
                {
                    Type = "LoanApplication",
                    Message = $"Underwriting Alert: {l.Member.FirstName} {l.Member.LastName} requested a loan of Ksh {l.LoanAmount:N0}.",
                    Timestamp = l.ApplicationDate
                })
                .ToListAsync();

            // Aggregate both task queues together, sorted by chronological urgency
            var adminFeed = pendingMembers
                .Concat(pendingLoans)
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            return Ok(adminFeed);
        }

        // =========================================================
        // MEMBER: GET MY NOTIFICATIONS
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member identity missing");

            var notifications = await _context.Notifications
                .Where(n => n.MemberId == memberId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        // =========================================================
        // MEMBER: MARK AS READ
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member identity missing");

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

        // =========================================================
        // MEMBER: UNREAD COUNT
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member identity missing");

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