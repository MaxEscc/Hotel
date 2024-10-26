using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HotelReservation.Models; 
using System.Threading.Tasks;

public class AccountController : Controller
{
    // Cambio de Identity a ApplicationUser para el registro de usuarios
    private readonly SignInManager<ApplicationUser> _signInManager; 
    private readonly UserManager<ApplicationUser> _userManager;    

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // GET: Account/Login
    [HttpGet]
    public IActionResult Login() => View();

    // POST: Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("ClientIndex", "Bookings");
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        return View(model);
    }

    // GET: Account/Register
    [HttpGet]
    public IActionResult Register() => View();

    // POST: Account/Register
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser 
            { 
                UserName = model.Email, 
                Email = model.Email,
                FirstName = model.FirstName, 
                LastName = model.LastName    
            }; 
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("ClientIndex", "Bookings");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    // Logout
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("ClientIndex", "Bookings");
    }
}
