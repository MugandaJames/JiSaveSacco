using System;
using System.Collections.Generic;

namespace JiSaveSacco.API.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        // Admin, Staff, Member
        public string Role { get; set; } = "Member";

        // Active, Inactive, Suspended
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // One-to-one relationship
        public Member? Member { get; set; }

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();

        public ICollection<Report> Reports { get; set; }
            = new List<Report>();
    }
}