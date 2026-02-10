using BanjirWatch.Models;

namespace BanjirWatch.Services.Interfaces;

/// <summary>
/// Service for managing flood data - coordinates between API service and repository
/// </summary>
public interface IFloodDataService
{
    /// <summary>
    /// Fetch flood data from API and store in database
    /// </summary>
    Task FetchAndStoreFloodDataAsync();
    
    /// <summary>
    /// Get active flood data from database
    /// </summary>
    Task<List<FloodData>> GetActiveFloodDataAsync(int limit = 100);
    
    /// <summary>
    /// Check flood risk at specific location
    /// </summary>
    Task<FloodRisk?> CheckLocationRiskAsync(double latitude, double longitude);
    
    /// <summary>
    /// Clean up expired flood data
    /// </summary>
    Task CleanupExpiredDataAsync();
    
    /// <summary>
    /// Get count of active alerts
    /// </summary>
    Task<int> GetActiveAlertsCountAsync();
}

public class FloodRisk
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Severity { get; set; }
    public double Precipitation { get; set; }
    public int WeatherCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}
