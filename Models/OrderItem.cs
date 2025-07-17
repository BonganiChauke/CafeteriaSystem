namespace CafeteriaSystem.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceAtTimeOfOrder { get; set; }
        public MenuItem MenuItem { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}
