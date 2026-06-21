namespace JiSaveSacco.API.DTOs.Savings
{
    public class SavingsResponseDto
    {
        public int SavingId { get; set; }

        public decimal Amount { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public decimal BalanceAfter { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}