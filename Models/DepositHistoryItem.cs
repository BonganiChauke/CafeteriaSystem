namespace CafeteriaSystem.Models
{
    public class DepositHistoryItem
    {
        public decimal Amount { get; set; }
        public DateTime DepositDate { get; set; }
        public string? TransactionType { get; internal set; }
    }
}
