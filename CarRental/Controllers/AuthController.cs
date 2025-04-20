using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CarRental.Models;
using System.Threading.Tasks;
using CarRental.ViewModels;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _context;


    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<AuthController> logger, AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _context = context; 
    }

    public IActionResult UserOrders()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Auth");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var orders = _context.Orders
            .Include(o => o.Car)
            .Where(o => o.UserId == userId)
            .ToList();

        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();

        return RedirectToAction("UserOrders");
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = OrderStatus.Confirmed;
        await _context.SaveChangesAsync();

        return RedirectToAction("UserOrders");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existingEmail = await _userManager.FindByNameAsync(model.Login);
            if (existingEmail != null)
            {
                ModelState.AddModelError(string.Empty, "Пользователь с таким login уже существует.");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Пользователь с таким email уже существует.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Login,
                Email = model.Email,
                PasswordHash = model.Password,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 3,
                RoleId = 1
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            await _userManager.AddToRoleAsync(user, "User");

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Регистрация прошла успешно! Теперь вы можете войти.";
                return RedirectToAction("Login", "Auth");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

 [HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (ModelState.IsValid)
    {
        var user = await _userManager.FindByNameAsync(model.UserName);
        
        _logger.LogInformation("Попытка входа пользователя: {Login}", $"{model.UserName} {model.Password}");
        _logger.LogInformation("Найденный пользователь: {UserId}, {UserName}", user?.Id, user?.UserName);

        if (user == null)
        {
            _logger.LogWarning("Пользователь {Login} не найден.", model.UserName);
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
        
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("Роли пользователя {UserId}: {Roles}", user.Id, string.Join(", ", roles));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("RoleId", user.RoleId.ToString()) 
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            await _userManager.RemoveClaimsAsync(user, await _userManager.GetClaimsAsync(user));
            await _userManager.AddClaimsAsync(user, claims);

            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("Пользователь {Login} успешно вошёл в систему. Роли: {Roles}", 
                model.UserName, string.Join(", ", roles));
                
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Учётная запись пользователя {Login} заблокирована.", model.UserName);
            ModelState.AddModelError(string.Empty, "Учётная запись заблокирована.");
        }
        else if (result.IsNotAllowed)
        {
            _logger.LogWarning("Пользователю {Login} запрещён вход.", model.UserName);
            ModelState.AddModelError(string.Empty, "Вход запрещён.");
        }
        else
        {
            _logger.LogWarning("Неудачная попытка входа для пользователя {Login}.", model.UserName);
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
        }
    }
    else
    {
        var errors = ModelState.Values.SelectMany(v => v.Errors);
        _logger.LogWarning("Модель данных не прошла валидацию. Ошибки: {Errors}", 
            string.Join(", ", errors.Select(e => e.ErrorMessage)));
    }

    return View(model);
}

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}