using System;

namespace JiSaveSacco.API.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        public int MemberId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Member Member { get; set; } = null!;
    }
}