public class LoanRepaymentResponseDto
{
    public int RepaymentId { get; set; }

    public int LoanId { get; set; }

    public decimal LoanAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal RemainingBalance { get; set; }

    public DateTime PaymentDate { get; set; }
}