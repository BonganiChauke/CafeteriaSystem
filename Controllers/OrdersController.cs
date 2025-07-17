using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CafeteriaSystem.Data;
using CafeteriaSystem.Models;
using CafeteriaSystem.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CafeteriaSystem.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IRestaurantService _restaurantService;

        // Single constructor
        public OrdersController(ApplicationDbContext context, IOrderService orderService, IRestaurantService restaurantService)
        {
            _context = context;
            _orderService = orderService;
            _restaurantService = restaurantService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }
            var orders = await _orderService.GetOrdersByUserIdAsync(userId);
            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Employee)
                .ToListAsync();
            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Employee)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Create()
        {
            var restaurants = await _restaurantService.GetRestaurantsAsync();
            ViewData["RestaurantId"] = new SelectList(restaurants, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Create(OrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
#pragma warning disable CS8601 // Possible null reference assignment.
                model.UserId = userId;
#pragma warning restore CS8601 // Possible null reference assignment.
                var result = await _orderService.PlaceOrderAsync(model);
                if (result.Success)
                {
                    return RedirectToAction(nameof(OrderConfirmation), new { orderId = result.OrderId });
                }
                ModelState.AddModelError("", result.ErrorMessage);
            }

            var restaurants = await _restaurantService.GetRestaurantsAsync();
            ViewData["RestaurantId"] = new SelectList(restaurants, "Id", "Name", model.RestaurantId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name", order.EmployeeId);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmployeeId,OrderDate,Status")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name", order.EmployeeId);
            return View(order);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}