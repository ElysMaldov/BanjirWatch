using BanjirWatch.Models;
using BanjirWatch.Repositories.Interfaces;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Services;

/// <summary>
/// Service for managing flood data - coordinates between API service and repository
/// </summary>
public class FloodDataService : IFloodDataService
{
    private readonly IFloodDataRepository _floodDataRepository;
    private readonly IWeatherApiService _weatherApiService;
    private readonly ILogger<FloodDataService> _logger;

    public FloodDataService(
        IFloodDataRepository floodDataRepository,
        IWeatherApiService weatherApiService,
        ILogger<FloodDataService> logger)
    {
        _floodDataRepository = floodDataRepository;
        _weatherApiService = weatherApiService;
        _logger = logger;
    }

    /// <summary>
    /// Fetch flood data from API and store in database
    /// </summary>
    public async Task FetchAndStoreFloodDataAsync()
    {
        try
        {
            _logger.LogInformation("Fetching flood data from API...");

            // Get DTOs from API service
            var floodDataDtos = await _weatherApiService.FetchFloodDataForMonitoredRegionsAsync();

            if (floodDataDtos.Any())
            {
                // Map DTOs to domain models
                var floodDataList = floodDataDtos.Select(dto => new FloodData
                {
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    Severity = dto.Severity,
                    Source = dto.Source,
                    Description = dto.Description,
                    Precipitation = dto.Precipitation,
                    CreatedAt = dto.CreatedAt,
                    ExpiresAt = dto.ExpiresAt
                }).ToList();

                await _floodDataRepository.AddRangeAsync(floodDataList);
                _logger.LogInformation("Stored {Count} flood data entries", floodDataList.Count);
            }

            // Clean up expired data
            await CleanupExpiredDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchAndStoreFloodDataAsync");
        }
    }

    /// <summary>
    /// Get active flood data from database
    /// </summary>
    public async Task<List<FloodData>> GetActiveFloodDataAsync(int limit = 100)
    {
        return await _floodDataRepository.GetActiveFloodDataAsync(limit);
    }

    /// <summary>
    /// Check flood risk at specific location
    /// </summary>
    public async Task<FloodRisk?> CheckLocationRiskAsync(double latitude, double longitude)
    {
        var floodRiskDto = await _weatherApiService.GetFloodRiskAsync(latitude, longitude);

        if (floodRiskDto == null)
            return null;

        // Map DTO to domain model
        return new FloodRisk
        {
            Latitude = floodRiskDto.Latitude,
            Longitude = floodRiskDto.Longitude,
            Severity = floodRiskDto.Severity,
            Precipitation = floodRiskDto.Precipitation,
            WeatherCode = floodRiskDto.WeatherCode,
            Description = floodRiskDto.Description,
            RiskLevel = floodRiskDto.RiskLevel
        };
    }

    /// <summary>
    /// Clean up expired flood data
    /// </summary>
    public async Task CleanupExpiredDataAsync()
    {
        await _floodDataRepository.CleanupExpiredAsync();
    }

    /// <summary>
    /// Get count of active alerts
    /// </summary>
    public async Task<int> GetActiveAlertsCountAsync()
    {
        return await _floodDataRepository.GetActiveCountAsync();
    }
}
