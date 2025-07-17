namespace CafeteriaSystem.Models
{
    public class OrderViewModel
    {
        public int RestaurantId { get; set; }
        public string EmployeeNumber { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }  
        public string UserId { get; set; }
    }
}
