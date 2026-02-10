using Microsoft.AspNetCore.Mvc;
using BanjirWatch.Models.ViewModels;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Controllers;

public class MapController : Controller
{
    private readonly IMapService _mapService;
    private readonly ILogger<MapController> _logger;

    public MapController(IMapService mapService, ILogger<MapController> logger)
    {
        _mapService = mapService;
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
        var floodPoints = await _mapService.GetFloodPointsAsync(lat, lon, radiusKm);
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
            var floodRisk = await _mapService.CheckLocationRiskAsync(lat, lon);
            
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
    /// Search for locations by name
    /// </summary>
    [HttpGet]
    public IActionResult SearchLocation(string query)
    {
        var results = _mapService.SearchLocation(query);
        return Json(results.Select(r => new { name = r.Name, lat = r.Lat, lon = r.Lon }));
    }
}
