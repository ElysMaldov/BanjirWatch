using System.Text.Json;
using BanjirWatch.DTOs;
using BanjirWatch.Services.Interfaces;

namespace BanjirWatch.Services;

/// <summary>
/// Service for communicating with Open-Meteo weather API
/// </summary>
public class WeatherApiService : IWeatherApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherApiService> _logger;

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

    public WeatherApiService(HttpClient httpClient, ILogger<WeatherApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://api.open-meteo.com/");
    }

    /// <summary>
    /// Get weather data for specific coordinates from Open-Meteo API
    /// </summary>
    public async Task<OpenMeteoResponseDto?> GetWeatherDataAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,precipitation,rain,showers,weather_code,cloud_cover,pressure_msl,surface_pressure,wind_speed_10m&timezone=auto&forecast_days=1";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OpenMeteoResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data for coordinates {Lat}, {Lon}", latitude, longitude);
            return null;
        }
    }

    /// <summary>
    /// Get flood risk for a specific location
    /// </summary>
    public async Task<FloodRiskDto?> GetFloodRiskAsync(double latitude, double longitude)
    {
        var weatherData = await GetWeatherDataAsync(latitude, longitude);
        
        if (weatherData?.Current == null)
            return null;

        var totalPrecipitation = (weatherData.Current.Precipitation ?? 0) 
            + (weatherData.Current.Rain ?? 0) 
            + (weatherData.Current.Showers ?? 0);

        var severity = CalculateSeverity(totalPrecipitation, weatherData.Current.WeatherCode ?? 0);

        return new FloodRiskDto
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

    /// <summary>
    /// Fetch and return flood data for all monitored regions
    /// </summary>
    public async Task<List<FloodDataDto>> FetchFloodDataForMonitoredRegionsAsync()
    {
        var floodDataList = new List<FloodDataDto>();

        foreach (var region in _monitoredRegions)
        {
            try
            {
                var weatherData = await GetWeatherDataAsync(region.Lat, region.Lon);
                
                if (weatherData?.Current != null)
                {
                    var precipitation = weatherData.Current.Precipitation ?? 0;
                    var rain = weatherData.Current.Rain ?? 0;
                    var showers = weatherData.Current.Showers ?? 0;
                    
                    var totalPrecipitation = precipitation + rain + showers;
                    var severity = CalculateSeverity(totalPrecipitation, weatherData.Current.WeatherCode ?? 0);

                    // Only include if there's significant precipitation or flood risk
                    if (severity > 0)
                    {
                        floodDataList.Add(new FloodDataDto
                        {
                            Latitude = region.Lat,
                            Longitude = region.Lon,
                            Severity = severity,
                            Source = "Open-Meteo",
                            Description = $"Precipitation: {totalPrecipitation:F1}mm. Weather: {GetWeatherDescription(weatherData.Current.WeatherCode ?? 0)}",
                            Precipitation = totalPrecipitation,
                            CreatedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddHours(6)
                        });
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

        return floodDataList;
    }

    /// <summary>
    /// Calculate flood severity based on precipitation and weather code
    /// </summary>
    private int CalculateSeverity(double precipitation, int weatherCode)
    {
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
}
