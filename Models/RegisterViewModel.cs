namespace CafeteriaSystem.Models
{
    public class RegisterViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
    }
}
