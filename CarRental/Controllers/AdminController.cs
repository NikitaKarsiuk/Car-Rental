using CarRental.Models;
using CarRental.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthController> _logger;



    public AdminController(AppDbContext context, UserManager<User> userManager, ILogger<AuthController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new AdminStatsViewModel
        {
            UserCount = await _context.Users.CountAsync(),
            CarCount = await _context.Cars.CountAsync(),
            OrderCount = await _context.Orders.CountAsync()
        };

        return View(stats);
    }
    #region Управление пользователями
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .ToListAsync();
        return View(users);
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        ViewBag.Roles = _context.Roles.ToList();
        return View();
    }

    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> CreateUser(RegisterViewModel model, int roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);

        if (ModelState.IsValid)
        {
            var user = new User
            {
                UserName = model.Login,
                Email = model.Email,
                RoleId = roleId 
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (role != null)
                {
                    await _userManager.AddToRoleAsync(user, role.Name);
                }

                TempData["Message"] = "Пользователь успешно создан";
                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = _context.Roles.ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        ViewBag.Roles = await _context.Roles.ToListAsync();

        ViewBag.Roles = _context.Roles.ToList();
        return View(new EditUserViewModel
        {
            Id = user.Id,
            Login = user.UserName,
            Email = user.Email,
            RoleId = user.RoleId
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.UserName = model.Login;
            user.Email = model.Email;
            user.RoleId = model.RoleId;

            var currentRoles = await _userManager.GetRolesAsync(user);
            var role = await _context.Roles.FindAsync(model.RoleId);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Message"] = "Изменения успешно сохранены";
                return RedirectToAction("Users");
            }

            foreach (var error in result.Errors)
            {
                _logger.LogError("Ошибка при сохранении изменений" + error.Description);
                ModelState.AddModelError(string.Empty, error.Description);
            }

        ViewBag.Roles = _context.Roles.ToList();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            _logger.LogInformation($"Попытка удаления пользователя ID: {id}");

            var user = await _userManager.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                _logger.LogWarning($"Пользователь ID: {id} не найден");
                TempData["Error"] = "Пользователь не найден";
                return RedirectToAction("Users");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                _logger.LogInformation($"Удаление ролей пользователя ID: {id}");
                var removeResult = await _userManager.RemoveFromRolesAsync(user, roles);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        _logger.LogError($"Ошибка удаления ролей: {error.Description}");
                    }
                }
            }

            _logger.LogInformation($"Удаление пользователя ID: {id}");
            var deleteResult = await _userManager.DeleteAsync(user);

            if (deleteResult.Succeeded)
            {
                _logger.LogInformation($"Пользователь ID: {id} успешно удален");
                TempData["Message"] = "Пользователь успешно удален";
            }
            else
            {
                foreach (var error in deleteResult.Errors)
                {
                    _logger.LogError($"Ошибка удаления пользователя: {error.Description}");
                }
                TempData["Error"] = "Не удалось удалить пользователя";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при удалении пользователя ID: {id}");
            TempData["Error"] = "Произошла ошибка при удалении пользователя";
        }

        return RedirectToAction("Users");
    }
    [HttpPost]
    public async Task<IActionResult> LockUser(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            TempData["Message"] = "Пользователь заблокирован";
        }
        return RedirectToAction("Users");
    }

    [HttpPost]
    public async Task<IActionResult> UnlockUser(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Message"] = "Пользователь разблокирован";
        }
        return RedirectToAction("Users");
    }
    #endregion

    #region Управление автомобилями
    public async Task<IActionResult> Cars()
    {
        var cars = await _context.Cars
            .Include(c => c.Category)
            .ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(cars);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCar()
    {
        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCar(CarViewModel model)
    {
        if (ModelState.IsValid)
        {
            var car = new Car
            {
                Name = model.Name,
                ShortDesc = model.ShortDesc,
                LongDesc = model.LongDesc,
                Price = model.Price,
                IsFavorite = model.IsFavorite,
                CategoryId = model.CategoryId,
                Img = await SaveFile(model.ImageFile)
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return RedirectToAction("Cars");
        }
        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(new CarViewModel
        {
            Id = car.Id,
            Name = car.Name,
            ShortDesc = car.ShortDesc,
            LongDesc = car.LongDesc,
            Price = car.Price,
            IsFavorite = car.IsFavorite,
            CategoryId = car.CategoryId,
            CurrentImagePath = car.Img
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCar(CarViewModel model)
    {
        ModelState.Remove("ImageFile");

        if (ModelState.IsValid)
        {
            var car = await _context.Cars.FindAsync(model.Id);
            if (car == null) return NotFound();

            car.Name = model.Name;
            car.ShortDesc = model.ShortDesc;
            car.LongDesc = model.LongDesc;
            car.Price = model.Price;
            car.IsFavorite = model.IsFavorite;
            car.CategoryId = model.CategoryId;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                car.Img = await SaveFile(model.ImageFile);
            }
            else
            {
                car.Img = model.CurrentImagePath;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Cars");
        }

        ViewBag.Categories = await _context.Categories.ToListAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car != null)
        {
            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Cars");
    }

    private async Task<string> SaveFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");

        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return "/img/" + uniqueFileName;
    }
    #endregion

    #region Управление заказами
    public async Task<IActionResult> Orders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Car)
            .OrderByDescending(o => o.DateFrom)
            .ToListAsync();
        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            order.Status = OrderStatus.Confirmed;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Orders");
    }

    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Orders");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Orders");
    }
    #endregion


}