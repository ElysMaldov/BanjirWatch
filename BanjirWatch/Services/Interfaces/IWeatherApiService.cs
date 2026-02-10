using BanjirWatch.DTOs;

namespace BanjirWatch.Services.Interfaces;

/// <summary>
/// Service for communicating with external weather/flood APIs
/// </summary>
public interface IWeatherApiService
{
    /// <summary>
    /// Get weather data for specific coordinates from Open-Meteo API
    /// </summary>
    Task<OpenMeteoResponseDto?> GetWeatherDataAsync(double latitude, double longitude);
    
    /// <summary>
    /// Get flood risk for a specific location
    /// </summary>
    Task<FloodRiskDto?> GetFloodRiskAsync(double latitude, double longitude);
    
    /// <summary>
    /// Fetch and return flood data for all monitored regions
    /// </summary>
    Task<List<FloodDataDto>> FetchFloodDataForMonitoredRegionsAsync();
}
