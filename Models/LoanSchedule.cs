using System;
using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class LoanSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        public int LoanId { get; set; }

        public DateTime DueDate { get; set; }

        public decimal AmountDue { get; set; }

        public string Status { get; set; } = "Pending";

        public Loan Loan { get; set; } = null!;
    }
}