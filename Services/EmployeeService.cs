using Microsoft.EntityFrameworkCore;
using CafeteriaSystem.Data;
using CafeteriaSystem.Models;


namespace CafeteriaSystem.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task<(bool Success, string ErrorMessage, int EmployeeId)> ProcessDepositAsync(string employeeNumber, decimal amount, string userId);
        Task AddEmployeeAsync(Employee employee);
    }

    public class EmployeeService : IEmployeeService
    {

        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return await _context.Employees.FindAsync(id);
#pragma warning restore CS8603 // Possible null reference return.
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Success, string ErrorMessage, int EmployeeId)> ProcessDepositAsync(string employeeNumber, decimal amount, string userId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);
            if (employee == null)
            {
                return (false, "Employee not found.", 0);
            }

            if (amount <= 0)
            {
                return (false, "Deposit amount must be greater than zero.", 0);
            }

            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Reset MonthlyDepositTotal if it's a new month
            if (employee.LastDepositMonth.Year != currentMonth.Year ||
                employee.LastDepositMonth.Month != currentMonth.Month)
            {
                employee.MonthlyDepositTotal = 0;
                employee.LastDepositMonth = currentMonth;
            }

            // Calculate bonus
            var previousTotal = employee.MonthlyDepositTotal;
            var newTotal = previousTotal + amount;
            var previousBonusCount = (int)(previousTotal / 250);
            var newBonusCount = (int)(newTotal / 250);
            var bonusAmount = (newBonusCount - previousBonusCount) * 500;

            // Update employee balance and monthly deposit total
            employee.Balance += amount + bonusAmount;
            employee.MonthlyDepositTotal += amount;

            // Record deposit in history
            var depositHistory = new DepositHistory
            {
                EmployeeId = employee.UserId,
                Amount = amount,
                DepositDate = DateTime.Now,
                TransactionType = "Deposit"
            };
            _context.DepositHistories.Add(depositHistory);

            // Record bonus in history if applicable
            if (bonusAmount > 0)
            {
                var bonusHistory = new DepositHistory
                {
                    EmployeeId = employee.UserId,
                    Amount = bonusAmount,
                    DepositDate = DateTime.Now,
                    TransactionType = "Bonus"
                };
                _context.DepositHistories.Add(bonusHistory);
            }

            try
            {
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();
                return (true, "", employee.Id);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to process deposit: {ex.Message}", 0);
            }
        }
    }
}



