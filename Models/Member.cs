using System;
using System.Collections.Generic;

namespace JiSaveSacco.API.Models
{
    public class Member
    {
        public int MemberId { get; set; }

        // Foreign key to User
        public int UserId { get; set; }

        public string MemberNo { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Occupation { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}";

        // Navigation to User
        public User User { get; set; } = null!;

        public ICollection<SavingsTransaction> SavingsTransactions { get; set; }
            = new List<SavingsTransaction>();

        public ICollection<Loan> Loans { get; set; }
            = new List<Loan>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        public ICollection<Guarantor> Guarantors { get; set; }
            = new List<Guarantor>();
    }
}