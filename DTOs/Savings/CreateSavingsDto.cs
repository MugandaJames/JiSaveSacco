namespace JiSaveSacco.API.DTOs.Savings
{
    public class CreateSavingsDto
    {
        public int MemberId { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}