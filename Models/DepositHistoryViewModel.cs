namespace CafeteriaSystem.Models
{
    public class DepositHistoryViewModel
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public List<DepositHistoryItem> Deposits { get; set; } = new List<DepositHistoryItem>();
        public decimal MonthlyDepositTotal { get; internal set; }
    }
}
