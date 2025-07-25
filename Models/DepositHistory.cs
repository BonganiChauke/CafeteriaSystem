
namespace CafeteriaSystem.Models
{
    public class DepositHistory
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty; 
        public decimal Amount { get; set; }
        public DateTime DepositDate { get; set; }
        public Employee Employee { get; set; } = null!;
        public decimal MonthlyDepositTotal { get; set; }
        public string? TransactionType { get; set; }
    }
}
