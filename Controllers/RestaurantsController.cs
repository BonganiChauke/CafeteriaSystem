using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CafeteriaSystem.Data;
using CafeteriaSystem.Models;
using CafeteriaSystem.Services;
using Microsoft.AspNetCore.Authorization;


namespace CafeteriaSystem.Controllers
{
    // Restricts access to this controller to users with the "Admin" role
    [Authorize(Roles = "Admin")]
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context; // Database context for entity operations
        private readonly IRestaurantService _restaurantService; // Service for restaurant-related business logic
        private readonly IOrderService _orderService; // Service for order-related business logic

        // constructor with dependency injection for context, restaurant, and order services
        public RestaurantsController(ApplicationDbContext context, IRestaurantService restaurantService, IOrderService orderService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure context is not null
            _restaurantService = restaurantService ?? throw new ArgumentNullException(nameof(restaurantService)); // Ensure restaurant service is not null
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService)); // Ensure order service is not null
        }

        // GET: /Restaurants
        // Displays a list of all restaurants, restricted to Admin role
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var restaurants = await _restaurantService.GetRestaurantsAsync(); // Fetch all restaurants via service
            return View(restaurants); // Render the Index view with the restaurant list
        }

        // GET: /Restaurants/Menu/{id}
        // Displays the menu for a restaurant, accessible to authorized users
        [Authorize]
        public async Task<IActionResult> Menu(int id)
        {
            var restaurant = await _restaurantService.GetRestaurantByIdAsync(id); // Fetch restaurant by ID
            if (restaurant == null) return NotFound(); // Return 404 if restaurant not found
            return View(restaurant); // Render the Menu view with restaurant details
        }

        // GET: /Restaurants/RestaurantMenu/{id}
        // Displays the menu items for a specific restaurant using EF Core
        public IActionResult RestaurantMenu(int id)
        {
            var restaurant = _context.Restaurants
                .Include(r => r.MenuItems) // Eager load menu items
                .FirstOrDefault(r => r.Id == id); // Fetch restaurant by ID

            if (restaurant == null)
            {
                return NotFound(); // Return 404 if restaurant not found
            }

            ViewData["RestaurantId"] = restaurant.Id; // Pass restaurant ID to view
            ViewData["RestaurantName"] = restaurant.Name; // Pass restaurant name to view

            return View(restaurant.MenuItems.ToList()); // Render the RestaurantMenu view with menu items
        }

        // GET: /Restaurants/ViewMenuItems/{id}
        // Displays menu items for a specific restaurant with debug logging
        [HttpGet]
        public async Task<IActionResult> ViewMenuItems(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems) // Eager load menu items
                .FirstOrDefaultAsync(r => r.Id == id); // Fetch restaurant by ID
            if (restaurant == null)
            {
                Console.WriteLine($"Restaurant not found for Id: {id}"); // Log error to console
                return NotFound($"Restaurant with ID {id} not found."); // Return 404 with message
            }

            Console.WriteLine($"ViewMenuItems: RestaurantId={id}, RestaurantName={restaurant.Name}, MenuItemsCount={restaurant.MenuItems.Count}"); // Debug log
            ViewData["RestaurantId"] = restaurant.Id; // Pass restaurant ID to view
            ViewData["RestaurantName"] = restaurant.Name; // Pass restaurant name to view

            var menuItems = restaurant.MenuItems.ToList(); // Convert to list for view
            return View(menuItems); // Render the ViewMenuItems view with menu items
        }

        // GET: /Restaurants/CreateMenuItem?restaurantId={restaurantId}
        // Displays a form to create a new menu item for a specific restaurant
        [HttpGet]
        public async Task<IActionResult> CreateMenuItem(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                Console.WriteLine($"Invalid RestaurantId: {restaurantId}"); // Log invalid ID
                return BadRequest("Invalid restaurant ID."); // Return 400 if ID is invalid
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId); // Fetch restaurant by ID
            if (restaurant == null)
            {
                Console.WriteLine($"Restaurant not found for Id: {restaurantId}"); // Log not found
                return NotFound($"Restaurant with ID {restaurantId} not found."); // Return 404
            }

            Console.WriteLine($"CreateMenuItem GET: RestaurantId={restaurantId}, RestaurantName={restaurant.Name}"); // Debug log
            ViewData["RestaurantId"] = restaurant.Id; // Pass restaurant ID to view
            ViewData["RestaurantName"] = restaurant.Name; // Pass restaurant name to view

            var model = new MenuItem
            {
                RestaurantId = restaurantId // Prepopulate restaurant ID
            };
            return View(model); // Render the CreateMenuItem view with the model
        }

        // POST: /Restaurants/CreateMenuItem
        // Handles the submission of a new menu item form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem([Bind("Name,Description,Price,RestaurantId")] MenuItem menuItem)
        {
            Console.WriteLine($"CreateMenuItem POST: RestaurantId={menuItem.RestaurantId}, RestaurantName={menuItem.Restaurant} Name={menuItem.Name}, Price={menuItem.Price}"); // Debug log

            if (menuItem.RestaurantId <= 0)
            {
                ModelState.AddModelError("RestaurantId", "Restaurant is required."); // Validate restaurant ID
            }
            else
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId); // Fetch restaurant
                if (restaurant == null)
                {
                    Console.WriteLine($"Restaurant not found for Id: {menuItem.RestaurantId}"); // Log not found
                    ModelState.AddModelError("RestaurantId", $"Restaurant with ID {menuItem.RestaurantId} not found."); // Add error
                }
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}"); // Log validation errors
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId); // Fetch restaurant for view
                ViewData["RestaurantId"] = menuItem.RestaurantId; // Pass ID to view
                ViewData["RestaurantName"] = restaurant?.Name ?? "Unknown"; // Pass name or default
                return View(menuItem); // Return to view with errors
            }

            try
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                menuItem.Restaurant = null; // Clear navigation property to avoid issues
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                _context.MenuItems.Add(menuItem); // Add new menu item
                await _context.SaveChangesAsync(); // Save changes to database
                Console.WriteLine($"MenuItem created: Name={menuItem.Name}, RestaurantId={menuItem.RestaurantId}"); // Log success
                return RedirectToAction(nameof(ViewMenuItems), new { id = menuItem.RestaurantId }); // Redirect to menu items view
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save menu item: {ex.Message}"); // Log exception
                ModelState.AddModelError("", $"Failed to create menu item: {ex.Message}"); // Add error to model
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId); // Fetch restaurant
                ViewData["RestaurantId"] = menuItem.RestaurantId; // Pass ID to view
                ViewData["RestaurantName"] = restaurant?.Name ?? "Unknown"; // Pass name or default
                return View(menuItem); // Return to view with error
            }
        }

        // GET: /Restaurants/Details/{id}
        // Displays details of a specific restaurant
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if ID is null
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.Id == id); // Fetch restaurant by ID
            if (restaurant == null)
            {
                return NotFound(); // Return 404 if restaurant not found
            }

            return View(restaurant); // Render the Details view with restaurant
        }

        // GET: /Restaurants/MenuItems/{id}
        // Displays menu items for a specific restaurant
        [HttpGet]
        public async Task<IActionResult> MenuItems(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems) // Eager load menu items
                .FirstOrDefaultAsync(r => r.Id == id); // Fetch restaurant by ID
            if (restaurant == null)
            {
                return NotFound(); // Return 404 if restaurant not found
            }
            ViewData["RestaurantId"] = id; // Pass restaurant ID to view
            ViewData["RestaurantName"] = restaurant.Name; // Pass restaurant name to view
            return View(restaurant.MenuItems); // Render the MenuItems view with menu items
        }

        // GET: /Restaurants/Edit/{id}
        // Displays a form to edit a specific restaurant
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if ID is null
            }

            var restaurant = await _context.Restaurants.FindAsync(id); // Fetch restaurant by ID
            if (restaurant == null)
            {
                return NotFound(); // Return 404 if restaurant not found
            }
            return View(restaurant); // Render the Edit view with restaurant
        }

        // POST: /Restaurants/Edit/{id}
        // Handles the submission of edited restaurant data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,LocationDescription,ContactNumber")] Restaurant restaurant)
        {
            if (id != restaurant.Id)
            {
                return NotFound(); // Return 404 if IDs don't match
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(restaurant); // Update restaurant in context
                    await _context.SaveChangesAsync(); // Save changes to database
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.Id))
                    {
                        return NotFound(); // Return 404 if restaurant no longer exists
                    }
                    else
                    {
                        throw; // Re-throw for upstream handling
                    }
                }
                return RedirectToAction(nameof(Index)); // Redirect to Index on success
            }
            return View(restaurant); // Return to Edit view with errors
        }

        // GET: /Restaurants/Delete/{id}
        // Displays a confirmation page for deleting a restaurant
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Return 404 if ID is null
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.Id == id); // Fetch restaurant by ID
            if (restaurant == null)
            {
                return NotFound(); // Return 404 if restaurant not found
            }

            return View(restaurant); // Render the Delete view with restaurant
        }

        // POST: /Restaurants/Delete/{id}
        // Handles the confirmed deletion of a restaurant
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id); // Fetch restaurant by ID
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant); // Remove restaurant from context
            }

            await _context.SaveChangesAsync(); // Save changes to database
            return RedirectToAction(nameof(Index)); // Redirect to Index
        }

        // GET: /Restaurants/EditMenuItem/{id}
        // Displays a form to edit a specific menu item
        [HttpGet]
        public async Task<IActionResult> EditMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id); // Fetch menu item by ID
            if (menuItem == null) return NotFound(); // Return 404 if menu item not found
            return View(menuItem); // Render the EditMenuItem view with menu item
        }

        // POST: /Restaurants/EditMenuItem/{id}
        // Handles the submission of edited menu item data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(int id, [Bind("Id,Name,Description,Price,RestaurantId")] MenuItem menuItem)
        {
            if (id != menuItem.Id) return NotFound(); // Return 404 if IDs don't match

            if (ModelState.IsValid)
            {
                _context.Update(menuItem); // Update menu item in context
                await _context.SaveChangesAsync(); // Save changes to database
                return RedirectToAction(nameof(ViewMenuItems), new { id = menuItem.RestaurantId }); // Redirect to menu items view
            }

            return View(menuItem); // Return to EditMenuItem view with errors
        }

        // GET: /Restaurants/DeleteMenuItem/{id}
        // Displays a confirmation page for deleting a menu item
        [HttpGet]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant) // Eager load restaurant
                .FirstOrDefaultAsync(m => m.Id == id); // Fetch menu item by ID

            if (menuItem == null) return NotFound(); // Return 404 if menu item not found
            return View(menuItem); // Render the DeleteMenuItem view with menu item
        }

        // POST: /Restaurants/DeleteMenuItem/{id}
        // Handles the confirmed deletion of a menu item
        [HttpPost, ActionName("DeleteMenuItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItemConfirmed(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id); // Fetch menu item by ID
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem); // Remove menu item from context
                await _context.SaveChangesAsync(); // Save changes to database
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return RedirectToAction(nameof(ViewMenuItems), new { id = menuItem.RestaurantId }); // Redirect to menu items view
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        // GET: /Restaurants/PlaceOrder
        // Displays a list of restaurants for placing an order, restricted to Employee role
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> PlaceOrder()
        {
            var restaurants = await _context.Restaurants.ToListAsync(); // Fetch all restaurants
            Console.WriteLine($"PlaceOrder: Retrieved {restaurants.Count} restaurants"); // Debug log
            return View(restaurants); // Render the PlaceOrder view with restaurants
        }

        // GET: /Restaurants/InitiateOrder
        // Initiates an order process with restaurant selection, restricted to Employee role
        // Renamed from Create to avoid ambiguity with restaurant creation
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> InitiateOrder(int? restaurantId, List<OrderItemViewModel> orderItems)
        {
            var restaurants = await _restaurantService.GetRestaurantsAsync(); // Fetch all restaurants via service
            var model = new OrderViewModel
            {
                RestaurantId = restaurantId ?? 0, // Default to 0 if no restaurant ID
                MenuItems = new List<MenuItem>(), // Initialize menu items list
                OrderItems = orderItems ?? new List<OrderItemViewModel>() // Initialize order items list or use provided
            };

            if (restaurantId.HasValue)
            {
                var restaurant = await _context.Restaurants
                    .Include(r => r.MenuItems) // Eager load menu items
                    .FirstOrDefaultAsync(r => r.Id == restaurantId.Value); // Fetch restaurant by ID
                if (restaurant != null)
                {
                    model.MenuItems = restaurant.MenuItems.ToList(); // Populate menu items
                }
            }

            ViewData["RestaurantId"] = new SelectList(restaurants ?? new List<Restaurant>(), "Id", "Name", restaurantId); // Set dropdown options
            return View(model); // Render the InitiateOrder view with the model
        }

        // POST: /Restaurants/Order
        // Handles the submission of an order for a specific restaurant, restricted to Employee role
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Order(int restaurantId, List<OrderItem> orderItems)
        {
            Console.WriteLine($"Order POST: RestaurantId={restaurantId}, OrderItemsCount={orderItems?.Count ?? 0}"); // Debug log

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId); // Fetch restaurant by ID
            if (restaurant == null)
            {
                Console.WriteLine($"Restaurant not found for Id: {restaurantId}"); // Log not found
                return NotFound($"Restaurant with ID {restaurantId} not found."); // Return 404
            }

            var orderItemViewModels = orderItems?.Where(oi => oi.Quantity > 0).Select(oi =>
            {
                var menuItem = _context.MenuItems.FirstOrDefault(m => m.Id == oi.MenuItemId); // Fetch menu item
                return new OrderItemViewModel
                {
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = menuItem?.Name ?? "Unknown", // Default to "Unknown" if null
                    UnitPrice = menuItem?.Price ?? oi.UnitPrice, // Use menu item price or provided
                    Quantity = oi.Quantity
                };
            }).ToList() ?? new List<OrderItemViewModel>(); // Convert to view models or default to empty list

            if (!orderItemViewModels.Any())
            {
                ModelState.AddModelError("", "Please select at least one menu item with a quantity greater than 0."); // Validate order items
            }

            decimal totalAmount = 0;
            foreach (var item in orderItemViewModels)
            {
                var menuItem = await _context.MenuItems
                    .FirstOrDefaultAsync(m => m.Id == item.MenuItemId && m.RestaurantId == restaurantId); // Fetch menu item
                if (menuItem == null)
                {
                    ModelState.AddModelError("", $"Menu item with ID {item.MenuItemId} is invalid or does not belong to the restaurant."); // Add error
                }
                else
                {
                    item.UnitPrice = menuItem.Price; // Set unit price
                    item.MenuItemName = menuItem.Name; // Set name
                    totalAmount += item.Quantity * item.UnitPrice; // Calculate total
                }
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value; // Get current user ID
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId); // Fetch employee
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}"); // Log not found
                return NotFound("Employee not found."); // Return 404
            }

            if (employee.Balance < totalAmount)
            {
                ModelState.AddModelError("", $"Insufficient balance. Available: {employee.Balance:C}, Required: {totalAmount:C}"); // Validate balance
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}"); // Log errors
                ViewData["RestaurantId"] = restaurantId; // Pass ID to view
                ViewData["RestaurantName"] = restaurant.Name; // Pass name to view
                var menuItems = await _context.MenuItems.Where(m => m.RestaurantId == restaurantId).ToListAsync(); // Fetch menu items
                return View("ViewMenuItems", menuItems); // Return to ViewMenuItems with errors
            }

            var orderViewModel = new OrderViewModel
            {
                RestaurantId = restaurantId,
                UserId = userId,
                EmployeeNumber = employee.EmployeeNumber, // Assuming Employee has this property
                OrderItems = orderItemViewModels,
                MenuItems = await _context.MenuItems.Where(m => m.RestaurantId == restaurantId).ToListAsync() // Populate menu items
            };

            try
            {
                var result = await _orderService.PlaceOrderAsync(orderViewModel); // Place the order
                if (result.Success)
                {
                    Console.WriteLine($"Order created: OrderId={result.OrderId}, EmployeeId={userId}, TotalAmount={totalAmount}"); // Log success
                    return RedirectToAction("OrderConfirmation", "Orders", new { orderId = result.OrderId }); // Redirect to order confirmation
                }
                ModelState.AddModelError("", result.ErrorMessage); // Add service error
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to place order: {ex.Message}"); // Log exception
                ModelState.AddModelError("", $"Failed to place order: {ex.Message}"); // Add error
            }

            ViewData["RestaurantId"] = restaurantId; // Pass ID to view
            ViewData["RestaurantName"] = restaurant.Name; // Pass name to view
            var menuItemsFallback = await _context.MenuItems.Where(m => m.RestaurantId == restaurantId).ToListAsync(); // Fetch menu items
            return View("ViewMenuItems", menuItemsFallback); // Return to ViewMenuItems with errors
        }

        // GET: /Restaurants/OrderConfirmation/{orderId}
        // Displays order confirmation for a specific order, restricted to Employee role
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Restaurant) // Eager load related data
                .FirstOrDefaultAsync(o => o.Id == orderId); // Fetch order by ID
            if (order == null)
            {
                return NotFound("Order not found."); // Return 404 if order not found
            }
            return View(order); // Render the OrderConfirmation view with order
        }

        // GET: /Restaurants/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,LocationDescription,ContactNumber")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                var newRestaurant = new Restaurant
                {
                    Name = restaurant.Name,
                    LocationDescription = restaurant.LocationDescription,
                    ContactNumber = restaurant.ContactNumber
                };
                _context.Add(newRestaurant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // method to check if a restaurant exists
        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.Id == id); // Check existence in database
        }
    }
}