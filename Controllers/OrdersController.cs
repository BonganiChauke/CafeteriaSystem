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
    [Authorize] // Ensures all actions require authentication
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context; // Database context for entity operations
        private readonly IOrderService _orderService; // Service for order-related business logic
        private readonly IRestaurantService _restaurantService; // Service for restaurant data

        // Constructor with dependency injection and null checks
        public OrdersController(ApplicationDbContext context, IOrderService orderService, IRestaurantService restaurantService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Throw if context is null
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService)); // Throw if service is null
            _restaurantService = restaurantService ?? throw new ArgumentNullException(nameof(restaurantService)); // Throw if service is null
        }

        // GET: ManageOrders (Admin only) - Displays all orders
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync(); // Fetch all orders
            return View(orders); // Render ManageOrders view
        }

        // GET: MyOrders (Employee only) - Displays current employee's orders
        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get current user ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated."); // Return unauthorized if no user
            }
            var orders = await _orderService.GetOrdersByUserIdAsync(userId); // Fetch user's orders
            return View(orders); // Render MyOrders view
        }

        // GET: Index (Admin only) - Lists all orders
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Employee) // Eager load Employee
                .ToListAsync(); // Fetch all orders
            return View(orders); // Render Index view
        }

        // GET: Details (Admin only) - Shows order details
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if ID is null
            }

            var order = await _context.Orders
                .Include(o => o.Employee) // Eager load Employee
                .Include(o => o.OrderItems) // Eager load OrderItems
                .ThenInclude(oi => oi.MenuItem) // Eager load MenuItem
                .FirstOrDefaultAsync(m => m.Id == id); // Fetch order by ID
            if (order == null)
            {
                return NotFound(); // Return 404 if order not found
            }

            return View(order); // Render Details view
        }

        // GET: Create (Employee only) - Displays order creation form
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Create(int? restaurantId = null)
        {
            // Fetch all restaurants for the dropdown
            var restaurants = await _restaurantService.GetRestaurantsAsync();
            if (restaurants == null)
            {
                restaurants = new List<Restaurant>(); // Fallback to empty list if null
                ModelState.AddModelError("", "No restaurants available."); // Log error
            }

            string restaurantName = "Unknown"; // Default restaurant name
            if (restaurantId.HasValue)
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == restaurantId.Value);
                if (restaurant != null)
                {
                    restaurantName = restaurant.Name; // Set restaurant name if found
                }
                else
                {
                    ModelState.AddModelError("", $"Restaurant with ID {restaurantId.Value} not found."); // Log error
                }
            }

            // Initialize OrderViewModel to prevent null reference in view
            var model = new OrderViewModel
            {
                RestaurantId = restaurantId ?? 0, // Default to 0 if no restaurantId
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value, // Set current user ID
                EmployeeNumber = string.Empty, // Initialize as empty string
                MenuItems = new List<MenuItem>(), // Explicit initialization
                OrderItems = new List<OrderItemViewModel>(), // Initialize to avoid null
                RestaurantOptions = new SelectList(restaurants, "Id", "Name", restaurantId) // Set restaurant options
            };

            // Load menu items and prepopulate order items if restaurantId is provided
            if (restaurantId.HasValue)
            {
                var restaurant = await _context.Restaurants
                    .Include(r => r.MenuItems) // Eager load MenuItems
                    .FirstOrDefaultAsync(r => r.Id == restaurantId.Value);
                if (restaurant != null)
                {
                    model.MenuItems = restaurant.MenuItems.ToList(); // Assign menu items
                    model.OrderItems = restaurant.MenuItems.Select(m => new OrderItemViewModel
                    {
                        MenuItemId = m.Id,
                        MenuItemName = m.Name,
                        UnitPrice = m.Price,
                        Quantity = 0 // Default quantity
                    }).ToList(); // Convert to OrderItemViewModel
                }
            }

            ViewData["RestaurantName"] = restaurantName; // Set restaurant name in ViewData
            return View(model); // Render Create view with model
        }

        // POST: Create (Employee only) - Processes order submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Create(OrderViewModel model)
        {
            // Ensure OrderItems is initialized if null to avoid runtime errors
            model.OrderItems ??= new List<OrderItemViewModel>();

            // Validate model state and check for valid order items
            if (ModelState.IsValid && model.OrderItems.Any(oi => oi.Quantity > 0))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError("", "User not authenticated."); // Add authentication error
                }
                else
                {
                    model.UserId = userId; // Assign user ID
                    var result = await _orderService.PlaceOrderAsync(model); // Attempt to place order
                    if (result.Success)
                    {
                        return RedirectToAction(nameof(OrderConfirmation), new { orderId = result.OrderId }); // Success redirect
                    }
                    ModelState.AddModelError("", result.ErrorMessage ?? "Failed to place order."); // Add service error
                }
            }
            else
            {
                ModelState.AddModelError("", "Please select at least one item with a quantity greater than 0."); // Validation error
            }

            // Repopulate restaurant options
            var restaurants = await _restaurantService.GetRestaurantsAsync() ?? new List<Restaurant>();
            model.RestaurantOptions = new SelectList(restaurants, "Id", "Name", model.RestaurantId);
            ViewData["RestaurantName"] = model.RestaurantId > 0
                ? (await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == model.RestaurantId))?.Name ?? "Unknown"
                : "Unknown";

            // Repopulate MenuItems and OrderItems if restaurant is selected and there’s an error
            if (model.RestaurantId > 0)
            {
                var restaurant = await _context.Restaurants
                    .Include(r => r.MenuItems) // Eager load MenuItems
                    .FirstOrDefaultAsync(r => r.Id == model.RestaurantId);
                if (restaurant != null)
                {
                    model.MenuItems = restaurant.MenuItems.ToList(); // Update menu items
                    // Preserve existing OrderItems with quantities or repopulate
                    model.OrderItems = model.OrderItems.Any(oi => oi.Quantity > 0)
                        ? model.OrderItems
                        : restaurant.MenuItems.Select(m => new OrderItemViewModel
                        {
                            MenuItemId = m.Id,
                            MenuItemName = m.Name,
                            UnitPrice = m.Price,
                            Quantity = 0
                        }).ToList();
                }
                else
                {
                    ModelState.AddModelError("", $"Restaurant with ID {model.RestaurantId} not found."); // Log error
                }
            }

            return View(model); // Return to Create view with errors and repopulated data
        }

        // GET: Edit (Admin only) - Displays edit form for an order
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if ID is null
            }

            var order = await _context.Orders.FindAsync(id); // Fetch order by ID
            if (order == null)
            {
                return NotFound(); // Return 404 if order not found
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name", order.EmployeeId); // Set employee dropdown
            return View(order); // Render Edit view
        }

        // POST: Edit (Admin only) - Updates an existing order
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmployeeId,OrderDate,Status")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound(); // Return 404 if IDs don't match
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order); // Update order in context
                    await _context.SaveChangesAsync(); // Save changes
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound(); // Return 404 if order no longer exists
                    }
                    else
                    {
                        throw; // Re-throw for upstream handling
                    }
                }
                return RedirectToAction(nameof(Index)); // Redirect to Index on success
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name", order.EmployeeId); // Repopulate dropdown
            return View(order); // Return to Edit view with errors
        }

        // GET: Delete (Admin only) - Shows delete confirmation
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if ID is null
            }

            var order = await _context.Orders
                .Include(o => o.Employee) // Eager load Employee
                .FirstOrDefaultAsync(m => m.Id == id); // Fetch order
            if (order == null)
            {
                return NotFound(); // Return 404 if order not found
            }

            return View(order); // Render Delete view
        }

        // POST: DeleteConfirmed (Admin only) - Executes order deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id); // Fetch order
            if (order != null)
            {
                _context.Orders.Remove(order); // Remove order
                await _context.SaveChangesAsync(); // Save changes
            }
            return RedirectToAction(nameof(Index)); // Redirect to Index
        }

        // GET: OrderConfirmation (Employee only) - Displays order confirmation
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId); // Fetch order by ID
            if (order == null)
            {
                return NotFound(); // Return 404 if order not found
            }
            return View(order); // Render OrderConfirmation view
        }

        // Helper method to check if an order exists
        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id); // Check order existence
        }
    }
}