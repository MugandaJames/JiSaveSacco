using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class Loan
    {
        [Key]
        public int LoanId { get; set; }

        public int MemberId { get; set; }

        public decimal LoanAmount { get; set; }

        public decimal InterestRate { get; set; }

        public decimal EligibleAmount { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovalDate { get; set; }

        public string Status { get; set; } = "Pending";

        public decimal OutstandingBalance { get; set; }

        public Member Member { get; set; } = null!;

        public ICollection<LoanRepayment> LoanRepayments { get; set; } = new List<LoanRepayment>();
        public ICollection<LoanSchedule> LoanSchedules { get; set; } = new List<LoanSchedule>();
        public ICollection<Guarantor> Guarantors { get; set; } = new List<Guarantor>();
    }
}