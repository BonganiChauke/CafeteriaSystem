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

        public IActionResult VieMenuItems(int id)
        {
            var restaurant = _context.Restaurants
        .Include(r => r.MenuItems)
        .FirstOrDefault(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            // Pass only the menu items to the view
            return View(restaurant.MenuItems.ToList());


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

        
        [HttpGet]
        public IActionResult CreateMenuItem(int restaurantId)
        {
            var model = new MenuItem { RestaurantId = restaurantId };
            return View(model);
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

        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
    }
}
