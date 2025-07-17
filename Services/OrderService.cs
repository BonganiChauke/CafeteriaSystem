using CafeteriaSystem.Data;
using CafeteriaSystem.Models;
using CafeteriaSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace CafeteriaSystem.Services
{
    public interface IOrderService
    {

        Task<List<Order>> GetAllOrdersAsync();
        Task<List<Order>> GetOrdersByUserIdAsync(string userId);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<(bool Success, int OrderId, string ErrorMessage)> PlaceOrderAsync(OrderViewModel model);
        Task UpdateOrderStatusAsync(int orderId, string status);

    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersByEmployeeNumberAsync(string employeeNumber)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);
            if (employee == null) return new List<Order>();
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.EmployeeId == employee.Id)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Employee)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Employee)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.Employee.UserId == userId)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Employee)
                .FirstOrDefaultAsync(o => o.Id == orderId);
#pragma warning restore CS8603 // Possible null reference return.
        }

        public async Task<(bool Success, int OrderId, string ErrorMessage)> PlaceOrderAsync(OrderViewModel model)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == model.UserId);
            if (employee == null)
            {
                return (false, 0, "Employee not found.");
            }

            var totalAmount = model.OrderItems.Sum(oi => oi.Quantity * oi.Price);
            if (employee.Balance < totalAmount)
            {
                return (false, 0, "Insufficient balance.");
            }

            var order = new Order
            {
                EmployeeId = employee.Id,
                OrderDate = DateTime.Now,
                Status = "Pending",
                OrderItems = model.OrderItems.Select(oi => new OrderItem
                {
                    MenuItemId = oi.MenuItemId,
                    Quantity = oi.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            employee.Balance -= totalAmount;
            _context.Employees.Update(employee);

            try
            {
                await _context.SaveChangesAsync();
                return (true, order.Id, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, $"Failed to place order: {ex.Message}");
            }
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
        }
    }
}
