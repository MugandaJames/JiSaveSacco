using System;
using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class LoanRepayment
    {
        [Key]
        public int RepaymentId { get; set; }

        public int LoanId { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public decimal RemainingBalance { get; set; }

        // Navigation
        public Loan Loan { get; set; } = null!;
    }
}