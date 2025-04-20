using System.ComponentModel.DataAnnotations;

namespace CarRental.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите дату начала аренды")]
        public DateTime DateFrom { get; set; }

        [Required(ErrorMessage = "Укажите дату окончания аренды")]
        public DateTime DateTo { get; set; }

        [Required(ErrorMessage = "Укажите ID пользователя")]
        public int? UserId { get; set; }

        public User? User { get; set; } 

        [Required(ErrorMessage = "Укажите ID автомобиля")]
        public int? CarId { get; set; }

        public Car? Car { get; set; }

        public decimal TotalCost { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending; 

    }
}
