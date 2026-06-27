namespace JiSaveSacco.API.DTOs.Loans
{
    public class CreateLoanDto
    {
        public int MemberId { get; set; }

        public decimal LoanAmount { get; set; }

        public decimal InterestRate { get; set; }

        public decimal EligibleAmount { get; set; }
    }
}