using JiSaveSacco.API.Data;
using JiSaveSacco.API.Models;

namespace JiSaveSacco.API.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Log(
            int? userId,
            string action,
            string tableAffected,
            int recordId)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                TableAffected = tableAffected,
                RecordId = recordId,
                ActionDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
        }
    }
}