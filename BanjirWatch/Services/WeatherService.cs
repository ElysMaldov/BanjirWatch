using System.Text.Json;
using BanjirWatch.Models;
using BanjirWatch.Data;
using Microsoft.EntityFrameworkCore;

namespace BanjirWatch.Services;

/// <summary>
/// Service for fetching weather and flood data from Open-Meteo API
/// </summary>
public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Regions to monitor for flood data (major cities around the world)
    private readonly List<(double Lat, double Lon, string Name)> _monitoredRegions = new()
    {
        // Indonesia
        (-6.2088, 106.8456, "Jakarta"),
        (-7.2575, 112.7521, "Surabaya"),
        (-6.9147, 107.6098, "Bandung"),
        (-6.9932, 110.4203, "Semarang"),
        (-8.6500, 115.2167, "Denpasar"),
        (3.5952, 98.6722, "Medan"),
        (-5.1477, 119.4327, "Makassar"),
        
        // Other major flood-prone cities
        (1.3521, 103.8198, "Singapore"),
        (13.7563, 100.5018, "Bangkok"),
        (14.5995, 120.9842, "Manila"),
        (3.1390, 101.6869, "Kuala Lumpur"),
        (10.8231, 106.6297, "Ho Chi Minh City"),
        (21.0285, 105.8542, "Hanoi"),
        
        // Major global cities
        (40.7128, -74.0060, "New York"),
        (51.5074, -0.1278, "London"),
        (35.6762, 139.6503, "Tokyo"),
        (-33.8688, 151.2093, "Sydney"),
        (19.0760, 72.8777, "Mumbai"),
        (28.6139, 77.2090, "New Delhi"),
        (31.2304, 121.4737, "Shanghai"),
        (55.7558, 37.6173, "Moscow"),
    };

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpClient.BaseAddress = new Uri("https://api.open-meteo.com/");
    }

    /// <summary>
    /// Fetch current weather and precipitation data for monitored regions
    /// </summary>
    public async Task FetchAndStoreFloodDataAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var floodDataList = new List<FloodData>();

            foreach (var region in _monitoredRegions)
            {
                try
                {
                    var weatherData = await GetWeatherDataAsync(region.Lat, region.Lon);
                    
                    if (weatherData != null)
                    {
                        // Check for heavy precipitation (potential flood risk)
                        var precipitation = weatherData.Current?.Precipitation ?? 0;
                        var rain = weatherData.Current?.Rain ?? 0;
                        var showers = weatherData.Current?.Showers ?? 0;
                        
                        // Calculate severity based on precipitation
                        var totalPrecipitation = precipitation + rain + showers;
                        var severity = CalculateSeverity(totalPrecipitation, weatherData.Current?.WeatherCode ?? 0);

                        // Only store if there's significant precipitation or flood risk
                        if (severity > 0)
                        {
                            var floodData = new FloodData
                            {
                                Latitude = region.Lat,
                                Longitude = region.Lon,
                                Severity = severity,
                                Source = "Open-Meteo",
                                Description = $"Precipitation: {totalPrecipitation:F1}mm. Weather: {GetWeatherDescription(weatherData.Current?.WeatherCode ?? 0)}",
                                Precipitation = totalPrecipitation,
                                CreatedAt = DateTime.UtcNow,
                                ExpiresAt = DateTime.UtcNow.AddHours(6) // Data expires after 6 hours
                            };

                            floodDataList.Add(floodData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching data for region {Region}", region.Name);
                }

                // Small delay to avoid rate limiting
                await Task.Delay(100);
            }

            // Store data in database
            if (floodDataList.Any())
            {
                dbContext.FloodData.AddRange(floodDataList);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Stored {Count} flood data entries", floodDataList.Count);
            }

            // Clean up expired data
            await CleanupExpiredDataAsync(dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchAndStoreFloodDataAsync");
        }
    }

    /// <summary>
    /// Get weather data for specific coordinates
    /// </summary>
    private async Task<OpenMeteoResponse?> GetWeatherDataAsync(double latitude, double longitude)
    {
        var url = $"/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,precipitation,rain,showers,weather_code,cloud_cover,pressure_msl,surface_pressure,wind_speed_10m&timezone=auto&forecast_days=1";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OpenMeteoResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Calculate flood severity based on precipitation and weather code
    /// </summary>
    private int CalculateSeverity(double precipitation, int weatherCode)
    {
        // WMO Weather interpretation codes
        // 61-67: Rain (slight, moderate, heavy)
        // 80-82: Rain showers (slight, moderate, violent)
        // 95-99: Thunderstorm with hail/sleet

        var severity = 0;

        // Base severity from precipitation amount
        if (precipitation >= 50) severity += 40;
        else if (precipitation >= 30) severity += 30;
        else if (precipitation >= 20) severity += 20;
        else if (precipitation >= 10) severity += 10;
        else if (precipitation >= 5) severity += 5;
        else if (precipitation > 0) severity += 2;

        // Additional severity from weather code
        severity += weatherCode switch
        {
            >= 95 => 30, // Thunderstorm
            >= 85 => 25, // Snow showers heavy
            >= 81 => 20, // Rain showers violent
            >= 80 => 15, // Rain showers
            >= 65 => 15, // Rain heavy
            >= 63 => 10, // Rain moderate
            >= 61 => 5,  // Rain slight
            >= 51 => 2,  // Drizzle
            _ => 0
        };

        return Math.Min(severity, 100); // Cap at 100
    }

    private string GetWeatherDescription(int weatherCode)
    {
        return weatherCode switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 => "Fog",
            48 => "Depositing rime fog",
            51 => "Light drizzle",
            53 => "Moderate drizzle",
            55 => "Dense drizzle",
            61 => "Slight rain",
            63 => "Moderate rain",
            65 => "Heavy rain",
            71 => "Slight snow",
            73 => "Moderate snow",
            75 => "Heavy snow",
            80 => "Slight rain showers",
            81 => "Moderate rain showers",
            82 => "Violent rain showers",
            95 => "Thunderstorm",
            96 => "Thunderstorm with slight hail",
            99 => "Thunderstorm with heavy hail",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Clean up expired flood data
    /// </summary>
    private async Task CleanupExpiredDataAsync(ApplicationDbContext dbContext)
    {
        var expiredData = await dbContext.FloodData
            .Where(f => f.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredData.Any())
        {
            dbContext.FloodData.RemoveRange(expiredData);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired flood data entries", expiredData.Count);
        }
    }

    /// <summary>
    /// Get current flood risk for a location
    /// </summary>
    public async Task<FloodRisk?> GetFloodRiskAsync(double latitude, double longitude)
    {
        try
        {
            var weatherData = await GetWeatherDataAsync(latitude, longitude);
            
            if (weatherData?.Current == null)
                return null;

            var totalPrecipitation = (weatherData.Current.Precipitation ?? 0) 
                + (weatherData.Current.Rain ?? 0) 
                + (weatherData.Current.Showers ?? 0);

            var severity = CalculateSeverity(totalPrecipitation, weatherData.Current.WeatherCode ?? 0);

            return new FloodRisk
            {
                Latitude = latitude,
                Longitude = longitude,
                Severity = severity,
                Precipitation = totalPrecipitation,
                WeatherCode = weatherData.Current.WeatherCode ?? 0,
                Description = GetWeatherDescription(weatherData.Current.WeatherCode ?? 0),
                RiskLevel = severity switch
                {
                    >= 60 => "High",
                    >= 30 => "Moderate",
                    >= 10 => "Low",
                    _ => "Minimal"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flood risk");
            return null;
        }
    }
}

// Open-Meteo API Response Models
public class OpenMeteoResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public CurrentWeather? Current { get; set; }
}

public class CurrentWeather
{
    public DateTime Time { get; set; }
    public int Interval { get; set; }
    public double? Temperature_2m { get; set; }
    public int? Relative_Humidity_2m { get; set; }
    public double? Precipitation { get; set; }
    public double? Rain { get; set; }
    public double? Showers { get; set; }
    public int? WeatherCode { get; set; }
    public int? CloudCover { get; set; }
    public double? Pressure_Msl { get; set; }
    public double? Surface_Pressure { get; set; }
    public double? Wind_Speed_10m { get; set; }
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
