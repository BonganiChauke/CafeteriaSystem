namespace CafeteriaSystem.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LocationDescription { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
