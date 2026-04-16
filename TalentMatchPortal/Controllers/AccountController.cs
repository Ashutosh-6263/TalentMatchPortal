using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TalentMatchPortal.Data;
using TalentMatchPortal.Models;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Register
    [HttpGet]
    public IActionResult Register() => View();

    // POST: Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(User user)
    {
        if (ModelState.IsValid)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }
        return View(user);
    }

    // GET: Login
    [HttpGet]
    public IActionResult Login(string role = "")
    {
        ViewBag.SelectedRole = role;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // 1. Search for ANY user in the database by Email and Password
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

        if (user != null)
        {
            // 2. Set Session Data (Used by _Layout.cshtml)
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FirstName);
            HttpContext.Session.SetInt32("UserId", user.Id);

            // 3. Create Authentication Cookie/Claims
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.FirstName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // 4. ROLE-BASED REDIRECTION
            // This logic now checks the 'Role' column in your database
            return user.Role switch
            {
                "HR" => RedirectToAction("Index", "HRDashboard"),
                "Lead" => RedirectToAction("Index", "LeadDashboard"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Employee" => RedirectToAction("Index", "EmployeeDashboard"),
                _ => RedirectToAction("List", "Jobs") // Default for Candidates
            };
        }

        // If no user found in database
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    // GET: Logout
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}