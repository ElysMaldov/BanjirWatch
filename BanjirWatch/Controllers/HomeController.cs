using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BanjirWatch.Models;
using BanjirWatch.Models.ViewModels;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Controllers;

public class HomeController : Controller
{
    private readonly IPostService _postService;
    private readonly IFloodDataService _floodDataService;
    private readonly IUserService _userService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IPostService postService,
        IFloodDataService floodDataService,
        IUserService userService,
        ILogger<HomeController> logger)
    {
        _postService = postService;
        _floodDataService = floodDataService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Get recent flood reports from service
        var recentReports = await _postService.GetRecentFloodReportsAsync(6);

        // Get statistics from services
        var stats = new HomeViewModel
        {
            TotalReports = await _postService.GetTotalPostsCountAsync(),
            ActiveAlerts = await _floodDataService.GetActiveAlertsCountAsync(),
            TotalUsers = await _userService.GetTotalUsersCountAsync(),
            RecentPosts = recentReports
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
