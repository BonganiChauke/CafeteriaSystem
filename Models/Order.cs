namespace CafeteriaSystem.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public Employee Employee { get; set; } = null!;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
