using Microsoft.AspNetCore.Mvc;
using CarRental.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging; 

namespace CarRental.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderController> _logger; 

        public OrderController(AppDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger; 
        }

        [HttpGet]
        public IActionResult Create(int carId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var car = _context.Cars
                .Include(c => c.Category)
                .FirstOrDefault(c => c.Id == carId);

            if (car == null)
            {
                return NotFound();
            }

            ViewBag.CarName = car.Name;
            ViewBag.CarPrice = car.Price;

            var order = new Order
            {
                CarId = carId,
                UserId = userId,
                DateFrom = DateTime.Today, 
                DateTo = DateTime.Today.AddDays(1)
            };

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            _logger.LogInformation("Received order: CarId={CarId}, UserId={UserId}, DateFrom={DateFrom}, DateTo={DateTo}",
                order.CarId, order.UserId, order.DateFrom, order.DateTo);

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Ошибка валидации: {ErrorMessage}", error.ErrorMessage);
                }

                return View(order);
            }

            if (ModelState.IsValid)
            {
                var car = await _context.Cars.FindAsync(order.CarId);
                if (car == null)
                {
                    return NotFound();
                }

                var days = (order.DateTo - order.DateFrom).Days;
                order.TotalCost = (decimal)car.Price * days;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}