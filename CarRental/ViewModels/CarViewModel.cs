using System.ComponentModel.DataAnnotations;

namespace CarRental.ViewModels
{
    public class CarViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Название")]
        public string Name { get; set; }
        [Display(Name = "Короткое описание")]

        public string ShortDesc { get; set; }
        [Display(Name = "Описание")]
        public string LongDesc { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Цена")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public double Price { get; set; }
        [Display(Name = "Избранное")]

        public bool IsFavorite { get; set; }
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }
        [Display(Name = "Изображение")]
        public IFormFile ImageFile { get; set; }
        public string CurrentImagePath { get; set; }
    }
}
