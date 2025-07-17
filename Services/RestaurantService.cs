using CafeteriaSystem.Data;
using CafeteriaSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeteriaSystem.Services
{
    public interface IRestaurantService
    {
        Task<List<Restaurant>> GetAllRestaurantsAsync();
        Task<Restaurant> GetRestaurantByIdAsync(int id);
        Task AddRestaurantAsync(Restaurant restaurant);
        Task AddMenuItemAsync(int restaurantId, MenuItem menuItem);
        Task<List<Restaurant>> GetRestaurantsAsync(); 
    }

    public class RestaurantService : IRestaurantService
    {
        private readonly ApplicationDbContext _context;

        public RestaurantService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Restaurant>> GetAllRestaurantsAsync()
        {
            return await _context.Restaurants.Include(r => r.MenuItems).ToListAsync();
        }

        public async Task<Restaurant> GetRestaurantByIdAsync(int id)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return await _context.Restaurants.Include(r => r.MenuItems).FirstOrDefaultAsync(r => r.Id == id);
#pragma warning restore CS8603 // Possible null reference return.
        }


        public async Task AddRestaurantAsync(Restaurant restaurant)
        {
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            return await _context.Restaurants.ToListAsync();
        }

        public async Task AddMenuItemAsync(int restaurantId, MenuItem menuItem)
        {
            menuItem.RestaurantId = restaurantId;
            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();
        }
    }
}
