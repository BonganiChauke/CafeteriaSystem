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
using System.Security.Claims;

namespace CafeteriaSystem.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeService _employeeService;

        public EmployeesController(ApplicationDbContext context, IEmployeeService employeeService)
        {
            _context = context;
            _employeeService = employeeService;
        }

        // List all employees
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            return View(employees);
        }

        // GET: Employees/Create
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddEmployeeAsync(employee);
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Deposit
        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Deposit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"User not authenticated.");
                return Unauthorized("User not authenticated.");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}");
                return NotFound("Employee record not found.");
            }

            Console.WriteLine($"Deposit GET: EmployeeNumber={employee.EmployeeNumber}, Balance={employee.Balance}, MonthlyDepositTotal={employee.MonthlyDepositTotal}");
            var model = new DepositViewModel
            {
                EmployeeNumber = employee.EmployeeNumber,
                CurrentBalance = employee.Balance,
                DepositAmount = 0
            };
            return View(model);
        }

        // POST: Employees/Deposit
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"User not authenticated.");
                return Unauthorized("User not authenticated.");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}");
                return NotFound("Employee record not found.");
            }

            model.EmployeeNumber = employee.EmployeeNumber;
            model.CurrentBalance = employee.Balance;

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState errors: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            if (model.DepositAmount <= 0)
            {
                ModelState.AddModelError("DepositAmount", "Deposit amount must be greater than zero.");
                return View(model);
            }

            if (model.EmployeeNumber != employee.EmployeeNumber)
            {
                ModelState.AddModelError("EmployeeNumber", "Invalid employee number.");
                return View(model);
            }

            // Process deposit using EmployeeService
            Console.WriteLine($"Before deposit: Balance={employee.Balance}, DepositAmount={model.DepositAmount}, MonthlyDepositTotal={employee.MonthlyDepositTotal}");
            var result = await _employeeService.ProcessDepositAsync(employee.EmployeeNumber, model.DepositAmount, userId);
            if (!result.Success)
            {
                Console.WriteLine($"Deposit failed: {result.ErrorMessage}");
                ModelState.AddModelError("", result.ErrorMessage);
                return View(model);
            }

            // Refresh employee data to get updated balance
            employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == result.EmployeeId);
            Console.WriteLine($"After deposit: Balance={employee?.Balance}, MonthlyDepositTotal={employee?.MonthlyDepositTotal}");

            return RedirectToAction(nameof(DepositHistory));
        }

        // GET: Employees/DepositHistory
        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> DepositHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"User not authenticated.");
                return Unauthorized("User not authenticated.");
            }

            var employee = await _context.Employees
                .Include(e => e.DepositHistories)
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
            {
                Console.WriteLine($"Employee not found for UserId: {userId}");
                return NotFound("Employee record not found.");
            }

            Console.WriteLine($"DepositHistory GET: EmployeeNumber={employee.EmployeeNumber}, Balance={employee.Balance}, MonthlyDepositTotal={employee.MonthlyDepositTotal}, Deposits={employee.DepositHistories.Count}");
            var model = new DepositHistoryViewModel
            {
                EmployeeNumber = employee.EmployeeNumber,
                CurrentBalance = employee.Balance,
                MonthlyDepositTotal = employee.MonthlyDepositTotal,
                Deposits = employee.DepositHistories.Select(d => new DepositHistoryItem
                {
                    Amount = d.Amount,
                    DepositDate = d.DepositDate,
                    TransactionType = d.TransactionType
                }).ToList()
            };
            return View(model);
        }

        // GET: Employees/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // GET: Employees/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,EmployeeNumber")] Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
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
            return View(employee);
        }

        // GET: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id); // Fixed from Restaurants
        }
    }
}