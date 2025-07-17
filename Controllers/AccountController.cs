using CafeteriaSystem.Data;
using CafeteriaSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CafeteriaSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // Create corresponding Employee record
                    var employee = new Employee
                    {
                        Name = model.Name,
                        EmployeeNumber = model.EmployeeNumber,
                        Balance = 0,
                        LastDepositMonth = DateTime.Now,
                        UserId = user.Id
                    };
                    _context.Employees.Add(employee);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Employees");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Console.WriteLine($"Logging out user: {User.Identity.Name}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            await _signInManager.SignOutAsync();
            Console.WriteLine("User signed out successfully.");
            return RedirectToAction("Index", "Home");
        }

        
    }
}
