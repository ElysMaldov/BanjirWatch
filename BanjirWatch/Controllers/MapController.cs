using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanjirWatch.Data;
using BanjirWatch.Models.ViewModels;
using BanjirWatch.Services;

namespace BanjirWatch.Controllers;

public class MapController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly WeatherService _weatherService;
    private readonly ILogger<MapController> _logger;

    public MapController(ApplicationDbContext context, WeatherService weatherService, ILogger<MapController> logger)
    {
        _context = context;
        _weatherService = weatherService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // Default view centered on user's location (or Jakarta as default)
        var viewModel = new MapViewModel
        {
            CenterLatitude = -6.2088, // Jakarta
            CenterLongitude = 106.8456,
            Zoom = 12
        };

        return View(viewModel);
    }

    /// <summary>
    /// Get flood points for the map (both API data and user reports)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFloodPoints(double? lat, double? lon, double? radiusKm)
    {
        var floodPoints = new List<MapPointViewModel>();

        // Get API flood data (not expired)
        var apiData = await _context.FloodData
            .Where(f => f.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(f => f.Severity)
            .Take(100)
            .ToListAsync();

        floodPoints.AddRange(apiData.Select(f => new MapPointViewModel
        {
            Id = f.Id,
            Latitude = f.Latitude,
            Longitude = f.Longitude,
            Title = $"Flood Risk: {f.Severity}%",
            Description = f.Description ?? "Flood risk detected",
            ImagePath = null,
            Type = "api",
            Severity = f.Severity,
            CreatedAt = f.CreatedAt
        }));

        // Get user reports with location data
        var userReports = await _context.Posts
            .Include(p => p.User)
            .Where(p => p.Latitude != null && p.Longitude != null)
            .Where(p => p.IsFloodReport)
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync();

        floodPoints.AddRange(userReports.Select(p => new MapPointViewModel
        {
            Id = p.Id,
            Latitude = p.Latitude!.Value,
            Longitude = p.Longitude!.Value,
            Title = $"Report by {p.User.Username}",
            Description = p.Content.Length > 100 ? p.Content[..100] + "..." : p.Content,
            ImagePath = p.ImagePath,
            Type = "user",
            Severity = p.Severity.HasValue ? (int)p.Severity.Value * 25 : 50, // Convert enum to percentage
            CreatedAt = p.CreatedAt
        }));

        // If radius specified, filter by distance
        if (lat.HasValue && lon.HasValue && radiusKm.HasValue)
        {
            floodPoints = floodPoints
                .Where(p => CalculateDistance(lat.Value, lon.Value, p.Latitude, p.Longitude) <= radiusKm.Value)
                .ToList();
        }

        return Json(floodPoints);
    }

    /// <summary>
    /// Check flood risk at specific location
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckLocation(double lat, double lon)
    {
        try
        {
            var floodRisk = await _weatherService.GetFloodRiskAsync(lat, lon);
            
            if (floodRisk == null)
            {
                return Json(new { error = "Unable to fetch weather data" });
            }

            return Json(new
            {
                severity = floodRisk.Severity,
                precipitation = floodRisk.Precipitation,
                description = floodRisk.Description,
                riskLevel = floodRisk.RiskLevel,
                weatherCode = floodRisk.WeatherCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking location {Lat}, {Lon}", lat, lon);
            return Json(new { error = "Error fetching data" });
        }
    }

    /// <summary>
    /// Search for locations by name (using simple hardcoded list for demo)
    /// In production, you might want to use a geocoding API
    /// </summary>
    [HttpGet]
    public IActionResult SearchLocation(string query)
    {
        var locations = new List<(string Name, double Lat, double Lon)>
        {
            ("Jakarta", -6.2088, 106.8456),
            ("Surabaya", -7.2575, 112.7521),
            ("Bandung", -6.9147, 107.6098),
            ("Semarang", -6.9932, 110.4203),
            ("Denpasar", -8.6500, 115.2167),
            ("Medan", 3.5952, 98.6722),
            ("Makassar", -5.1477, 119.4327),
            ("Singapore", 1.3521, 103.8198),
            ("Bangkok", 13.7563, 100.5018),
            ("Manila", 14.5995, 120.9842),
            ("Kuala Lumpur", 3.1390, 101.6869),
            ("Ho Chi Minh City", 10.8231, 106.6297),
            ("Hanoi", 21.0285, 105.8542),
            ("New York", 40.7128, -74.0060),
            ("London", 51.5074, -0.1278),
            ("Tokyo", 35.6762, 139.6503),
            ("Sydney", -33.8688, 151.2093),
            ("Mumbai", 19.0760, 72.8777),
            ("New Delhi", 28.6139, 77.2090),
            ("Shanghai", 31.2304, 121.4737),
        };

        var results = locations
            .Where(l => l.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(l => new { name = l.Name, lat = l.Lat, lon = l.Lon })
            .Take(5)
            .ToList();

        return Json(results);
    }

    /// <summary>
    /// Calculate distance between two coordinates using Haversine formula
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
