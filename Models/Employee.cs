namespace CafeteriaSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime LastDepositMonth { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal MonthlyDepositTotal { get; set; }
        public List<DepositHistory> DepositHistories { get; set; } = new List<DepositHistory>();
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
