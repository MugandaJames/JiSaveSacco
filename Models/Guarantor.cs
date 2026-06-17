using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class Guarantor
    {
        [Key]
        public int GuarantorId { get; set; }

        public int LoanId { get; set; }

        public int MemberId { get; set; }

        public decimal GuaranteedAmount { get; set; }

        public Loan Loan { get; set; } = null!;
        public Member Member { get; set; } = null!;
    }
}