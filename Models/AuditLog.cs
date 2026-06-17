using System;
using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string TableAffected { get; set; } = string.Empty;

        public int RecordId { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}