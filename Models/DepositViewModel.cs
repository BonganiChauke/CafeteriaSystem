namespace CafeteriaSystem.Models
{
    public class DepositViewModel
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal DepositAmount { get; set; }
    }
}
