
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CafeteriaSystem.Models
{
    public class OrderViewModel
    {
        public int RestaurantId { get; set; }
        public string? EmployeeNumber { get; set; }
        public List<OrderItemViewModel>? OrderItems { get; set; }  
        public string? UserId { get; set; }
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public int MenuItemId { get; set; }
        public string? MenuItemName { get; set; }
        public decimal UnitPrice { get; set; } 
        public int Quantity { get; set; }
        public SelectList? RestaurantOptions { get; set; }
        public List<OrderItemViewModel>? CartItems { get; set; }
    }
}
