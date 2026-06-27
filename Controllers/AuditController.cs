using JiSaveSacco.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Kept secure exclusively for administration level accounts
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditTrail()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.User) // Includes relation tracking details if defined
                .OrderByDescending(a => a.ActionDate)
                .Select(a => new
                {
                    AuditLogId = a.LogId, // Maps onto whatever identity key field your model specifies
                    Username = a.User != null ? a.User.Username : $"User ID: {a.UserId}",
                    a.Action,
                    a.TableAffected,
                    a.RecordId,
                    a.ActionDate
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}