namespace CarRental.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortDesc { get; set; }
        public string LongDesc { get; set; }
        public string Img { get; set; }
        public double Price { get; set; }
        public bool IsFavorite { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
