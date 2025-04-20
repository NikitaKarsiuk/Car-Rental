namespace CarRental.Models
{
    public class CarFilter
    {
        public int? CategoryId { get; set; }
        public List<Car> Cars { get; set; }
        public List<Category> Categories { get; set; } 
    }
}
