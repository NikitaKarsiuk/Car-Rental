using CarRental.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var currentDate = DateTime.Today;

        var availableFavoriteCars = _context.Cars
            .Include(c => c.Category)
            .Where(c => c.IsFavorite && 
                        !_context.Orders
                            .Any(o => o.CarId == c.Id &&
                                      o.Status == OrderStatus.Confirmed &&
                                      o.DateFrom <= currentDate &&
                                      o.DateTo >= currentDate))
            .ToList();

        return View(availableFavoriteCars);
    }
}