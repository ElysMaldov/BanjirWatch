using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using BanjirWatch.Models;
using BanjirWatch.Models.ViewModels;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Controllers;

public class AuthController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IUserService userService, 
        ILogger<AuthController> logger, 
        IWebHostEnvironment environment)
    {
        _userService = userService;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Authenticate user through service
        var user = await _userService.AuthenticateAsync(model.UsernameOrEmail, model.Password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password");
            return View(model);
        }

        // Update last login
        await _userService.UpdateLastLoginAsync(user.Id);

        // Create authentication claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("AvatarPath", user.AvatarPath ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe 
                ? DateTimeOffset.UtcNow.AddDays(30) 
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Username} logged in", user.Username);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Register user through service
        var (user, success, error) = await _userService.RegisterAsync(model.Username, model.Email, model.Password);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        // Auto login after registration
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = false });

        TempData["SuccessMessage"] = "Registration successful! Welcome to BanjirWatch.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Profile()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", new { returnUrl = Url.Action("Profile") });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        // Use service to get user with posts
        var user = _userService.GetByIdWithPostsAsync(userId).Result;

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        if (avatar == null || avatar.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload";
            return RedirectToAction("Profile");
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(avatar.ContentType))
        {
            TempData["ErrorMessage"] = "Only image files (JPG, PNG, GIF, WebP) are allowed";
            return RedirectToAction("Profile");
        }

        if (avatar.Length > 5 * 1024 * 1024) // 5MB limit
        {
            TempData["ErrorMessage"] = "File size must be less than 5MB";
            return RedirectToAction("Profile");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        using var stream = avatar.OpenReadStream();
        var avatarPath = await _userService.UpdateAvatarAsync(userId, stream, avatar.FileName, avatar.ContentType);

        if (avatarPath == null)
        {
            TempData["ErrorMessage"] = "Failed to upload avatar";
            return RedirectToAction("Profile");
        }

        TempData["SuccessMessage"] = "Avatar updated successfully";
        return RedirectToAction("Profile");
    }
}
