using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRental.Models;
using System.Linq;

public class CarsController : Controller
{
    private readonly AppDbContext _context;

    public CarsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var currentDate = DateTime.Today;

        var cars = _context.Cars
            .Include(c => c.Category)
            .Where(c => !_context.Orders
                .Any(o => o.CarId == c.Id &&
                          o.Status == OrderStatus.Confirmed &&
                          o.DateFrom <= currentDate &&
                          o.DateTo >= currentDate)) 
            .ToList();

        return View(cars);
    }

    public IActionResult List(int? categoryId)
    {
        var currentDate = DateTime.Today;

        var cars = _context.Cars
            .Include(c => c.Category)
            .Where(c => !categoryId.HasValue || c.CategoryId == categoryId) 
            .Where(c => !_context.Orders
                .Any(o => o.CarId == c.Id &&
                          o.Status == OrderStatus.Confirmed &&
                          o.DateFrom <= currentDate &&
                          o.DateTo >= currentDate)) 
            .ToList();

        var categories = _context.Categories.ToList();

        var model = new CarFilter
        {
            Cars = cars,
            Categories = categories,
            CategoryId = categoryId
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .Include(c => c.Category)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }
}