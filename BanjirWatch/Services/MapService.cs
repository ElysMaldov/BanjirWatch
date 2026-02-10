using BanjirWatch.Models.ViewModels;
using BanjirWatch.Repositories.Interfaces;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Services;

/// <summary>
/// Service for map-related operations
/// </summary>
public class MapService : IMapService
{
    private readonly IFloodDataRepository _floodDataRepository;
    private readonly IPostRepository _postRepository;
    private readonly IWeatherApiService _weatherApiService;

    // Predefined locations for search
    private readonly List<(string Name, double Lat, double Lon)> _predefinedLocations = new()
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

    public MapService(
        IFloodDataRepository floodDataRepository,
        IPostRepository postRepository,
        IWeatherApiService weatherApiService)
    {
        _floodDataRepository = floodDataRepository;
        _postRepository = postRepository;
        _weatherApiService = weatherApiService;
    }

    public async Task<List<MapPointViewModel>> GetFloodPointsAsync(double? lat = null, double? lon = null, double? radiusKm = null)
    {
        var floodPoints = new List<MapPointViewModel>();

        // Get API flood data from repository
        var apiData = await _floodDataRepository.GetActiveFloodDataAsync(100);
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

        // Get user reports with location data from repository
        var userReports = await _postRepository.GetFloodReportsAsync(100);
        floodPoints.AddRange(userReports.Select(p => new MapPointViewModel
        {
            Id = p.Id,
            Latitude = p.Latitude!.Value,
            Longitude = p.Longitude!.Value,
            Title = $"Report by {p.User.Username}",
            Description = p.Content.Length > 100 ? p.Content[..100] + "..." : p.Content,
            ImagePath = p.ImagePath,
            Type = "user",
            Severity = p.Severity.HasValue ? (int)p.Severity.Value * 25 : 50,
            CreatedAt = p.CreatedAt
        }));

        // If radius specified, filter by distance
        if (lat.HasValue && lon.HasValue && radiusKm.HasValue)
        {
            floodPoints = floodPoints
                .Where(p => CalculateDistance(lat.Value, lon.Value, p.Latitude, p.Longitude) <= radiusKm.Value)
                .ToList();
        }

        return floodPoints;
    }

    public async Task<LocationRiskResult?> CheckLocationRiskAsync(double lat, double lon)
    {
        var floodRiskDto = await _weatherApiService.GetFloodRiskAsync(lat, lon);
        
        if (floodRiskDto == null)
            return null;

        return new LocationRiskResult
        {
            Severity = floodRiskDto.Severity,
            Precipitation = floodRiskDto.Precipitation,
            Description = floodRiskDto.Description,
            RiskLevel = floodRiskDto.RiskLevel,
            WeatherCode = floodRiskDto.WeatherCode
        };
    }

    public List<LocationSearchResult> SearchLocation(string query)
    {
        return _predefinedLocations
            .Where(l => l.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(l => new LocationSearchResult
            {
                Name = l.Name,
                Lat = l.Lat,
                Lon = l.Lon
            })
            .Take(5)
            .ToList();
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
