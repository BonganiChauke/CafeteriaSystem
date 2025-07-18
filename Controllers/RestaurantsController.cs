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
    [Authorize(Roles = "Admin")]
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantService _restaurantService;

        // Single constructor
        public RestaurantsController(ApplicationDbContext context, IRestaurantService restaurantService)
        {
            _context = context;
            _restaurantService = restaurantService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var restaurants = await _restaurantService.GetRestaurantsAsync();
            return View(restaurants);
        }


        [Authorize]
        public async Task<IActionResult> Menu(int id)
        {
            var restaurant = await _restaurantService.GetRestaurantByIdAsync(id);
            if (restaurant == null) return NotFound();
            return View(restaurant);
        }



        public IActionResult RestaurantMenu(int id)
        {

            var restaurant = _context.Restaurants
         .Include(r => r.MenuItems)
         .FirstOrDefault(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            ViewData["RestaurantId"] = restaurant.Id;
            ViewData["RestaurantName"] = restaurant.Name;


            // Pass only the menu items to the view
            return View(restaurant.MenuItems.ToList());
        }


        [HttpGet]
        public async Task<IActionResult> ViewMenuItems(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (restaurant == null)
            {
                Console.WriteLine($"Restaurant not found for Id: {id}");
                return NotFound($"Restaurant with ID {id} not found.");
            }

            Console.WriteLine($"ViewMenuItems: RestaurantId={id}, RestaurantName={restaurant.Name}, MenuItemsCount={restaurant.MenuItems.Count}");
            ViewData["RestaurantId"] = restaurant.Id;
            ViewData["RestaurantName"] = restaurant.Name;

            var menuItems = restaurant.MenuItems.ToList();
            return View(menuItems);
        }

        // GET: Restaurants/CreateMenuItem?restaurantId=5
        [HttpGet]
        public async Task<IActionResult> CreateMenuItem(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                Console.WriteLine($"Invalid RestaurantId: {restaurantId}");
                return BadRequest("Invalid restaurant ID.");
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId);
            if (restaurant == null)
            {
                Console.WriteLine($"Restaurant not found for Id: {restaurantId}");
                return NotFound($"Restaurant with ID {restaurantId} not found.");
            }

            Console.WriteLine($"CreateMenuItem GET: RestaurantId={restaurantId}, RestaurantName={restaurant.Name}");
            ViewData["RestaurantId"] = restaurant.Id;
            ViewData["RestaurantName"] = restaurant.Name;

            var model = new MenuItem
            {
                RestaurantId = restaurantId
            };
            return View(model);
        }

        // GET: Restaurants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            return View(restaurant);
        }

        
        [HttpGet]
        public async Task<IActionResult> MenuItems(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }
            ViewData["RestaurantId"] = id;
            ViewData["RestaurantName"] = restaurant.Name;
            return View(restaurant.MenuItems);
        }


        // POST: Restaurants/CreateMenuItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem([Bind("Name,Description,Price,RestaurantId")] MenuItem menuItem)
        {
            Console.WriteLine($"CreateMenuItem POST: RestaurantId={menuItem.RestaurantId}, RestaurantName={menuItem.Restaurant} Name={menuItem.Name}, Price={menuItem.Price}");

            if (menuItem.RestaurantId <= 0)
            {
                ModelState.AddModelError("RestaurantId", "Restaurant is required.");
            }
            else
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId);
                if (restaurant == null)
                {
                    Console.WriteLine($"Restaurant not found for Id: {menuItem.RestaurantId}");
                    ModelState.AddModelError("RestaurantId", $"Restaurant with ID {menuItem.RestaurantId} not found.");
                }
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"ModelState errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId);
                ViewData["RestaurantId"] = menuItem.RestaurantId;
                ViewData["RestaurantName"] = restaurant?.Name ?? "Unknown";
                return View(menuItem);
            }

            try
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                menuItem.Restaurant = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();
                Console.WriteLine($"MenuItem created: Name={menuItem.Name}, RestaurantId={menuItem.RestaurantId}");
                return RedirectToAction(nameof(ViewMenuItems), new { id = menuItem.RestaurantId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save menu item: {ex.Message}");
                ModelState.AddModelError("", $"Failed to create menu item: {ex.Message}");
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId);
                ViewData["RestaurantId"] = menuItem.RestaurantId;
                ViewData["RestaurantName"] = restaurant?.Name ?? "Unknown";
                return View(menuItem);
            }
        }
       

        // GET: Restaurants/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Restaurants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,LocationDescription,ContactNumber")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                // Fixing CS0103: The name 'model' does not exist in the current context  
                // The variable 'model' does not exist, but 'restaurant' is already passed as a parameter.  
                // Use the 'restaurant' parameter directly instead of 'model'.  
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


        // GET: Restaurants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                return NotFound();
            }
            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,LocationDescription,ContactNumber")] Restaurant restaurant)
        {
            if (id != restaurant.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.Id))
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
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //menu item
        [HttpGet]
        public async Task<IActionResult> EditMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null) return NotFound();
            return View(menuItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(int id, [Bind("Id,Name,Description,Price,RestaurantId")] MenuItem menuItem)
        {
            if (id != menuItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(menuItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewMenuItems), new { id = menuItem.RestaurantId });
            }

            return View(menuItem);
        }

        //
        [HttpGet]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();
            return View(menuItem);
        }

        [HttpPost, ActionName("DeleteMenuItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItemConfirmed(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return RedirectToAction(nameof(ViewMenuItems), new { id = menuItem.RestaurantId });
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }


        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
    }
}
