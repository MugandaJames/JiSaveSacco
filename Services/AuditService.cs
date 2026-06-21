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

        public async Task Log(int? userId, string action, string table, int recordId)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                TableAffected = table,
                RecordId = recordId,
                ActionDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}