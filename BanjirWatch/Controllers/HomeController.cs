using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Models;
using BanjirWatch.Models.ViewModels;

namespace BanjirWatch.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Get recent flood reports
        var recentReports = await _context.Posts
            .Include(p => p.User)
            .Where(p => p.IsFloodReport)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToListAsync();

        // Get statistics
        var stats = new HomeViewModel
        {
            TotalReports = await _context.Posts.CountAsync(p => p.IsFloodReport),
            ActiveAlerts = await _context.FloodData.CountAsync(f => f.ExpiresAt > DateTime.UtcNow),
            TotalUsers = await _context.Users.CountAsync(),
            RecentPosts = recentReports.Select(p => new PostViewModel
            {
                Id = p.Id,
                Content = p.Content.Length > 150 ? p.Content[..150] + "..." : p.Content,
                ImagePath = p.ImagePath,
                LocationName = p.LocationName,
                CreatedAt = p.CreatedAt,
                Username = p.User.Username,
                AvatarPath = p.User.AvatarPath,
                LikesCount = p.Likes.Count,
                CommentsCount = p.Comments.Count
            }).ToList()
        };

        return View(stats);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class HomeViewModel
{
    public int TotalReports { get; set; }
    public int ActiveAlerts { get; set; }
    public int TotalUsers { get; set; }
    public List<PostViewModel> RecentPosts { get; set; } = new();
}
