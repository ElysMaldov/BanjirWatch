using BanjirWatch.Models.ViewModels;

namespace BanjirWatch.Services.Interfaces;

/// <summary>
/// Service for map-related operations
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Get all flood points (from API data and user reports)
    /// </summary>
    Task<List<MapPointViewModel>> GetFloodPointsAsync(double? lat = null, double? lon = null, double? radiusKm = null);
    
    /// <summary>
    /// Check flood risk at specific location
    /// </summary>
    Task<LocationRiskResult?> CheckLocationRiskAsync(double lat, double lon);
    
    /// <summary>
    /// Search for locations by name
    /// </summary>
    List<LocationSearchResult> SearchLocation(string query);
}

public class LocationRiskResult
{
    public int Severity { get; set; }
    public double Precipitation { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int WeatherCode { get; set; }
}

public class LocationSearchResult
{
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
}
