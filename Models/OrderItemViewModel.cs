namespace CafeteriaSystem.Models
{
    public class OrderItemViewModel
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }  
        public decimal UnitPrice { get; internal set; }
        public string? MenuItemName { get; internal set; }
        
    }
}
