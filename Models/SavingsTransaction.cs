using System;
using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class SavingsTransaction
    {
        [Key]
        public int SavingId { get; set; }

        public int MemberId { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public Member Member { get; set; } = null!;
    }
}